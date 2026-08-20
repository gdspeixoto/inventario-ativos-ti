using ControleAtivos.Application.Abstractions;
using ControleAtivos.Application.Common;
using ControleAtivos.Domain.Entities;
using ControleAtivos.Domain.Enums;
using ControleAtivos.Domain.ValueObjects;

namespace ControleAtivos.Application.Features.Licenses;

/// <summary>
/// Casos de uso de licencas.
///
/// Todo metodo comeca resolvendo o escopo organizacional do usuario e valida o
/// acesso a unidade envolvida. O filtro de tenant vem do <c>DbContext</c>; a
/// verificacao de escopo e feita aqui porque depende do papel do usuario, nao
/// apenas da organizacao a que pertence.
/// </summary>
public sealed class LicenseService(
    IAssetQueries queries,
    IAssetRepository assets,
    IOrganizationalUnitRepository organizationalUnits,
    ISupplierRepository suppliers,
    IContractRepository contracts,
    ICostCenterRepository costCenters,
    IUserRepository users,
    IPriceAdjustmentRepository priceAdjustments,
    IOrganizationalScopeResolver scopeResolver,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
{
    /// <summary>
    /// Percentual a partir do qual um reajuste e sinalizado para revisao.
    /// Sera movido para as configuracoes do tenant quando a tela existir.
    /// </summary>
    public const decimal DefaultAdjustmentAlertThreshold = 10m;

    public async Task<PagedResult<LicenseListItemDto>> ListAsync(
        ListLicensesQuery query,
        CancellationToken cancellationToken = default)
    {
        var scope = await scopeResolver.GetAsync(cancellationToken);

        // Sem escopo nao ha o que listar; evita ida ao banco.
        if (scope.IsEmpty)
        {
            return PagedResult<LicenseListItemDto>.Empty(query.Page, query.PageSize);
        }

        return await queries.ListLicensesAsync(query, scope, cancellationToken);
    }

    public async Task<LicenseDetailDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scope = await scopeResolver.GetAsync(cancellationToken);

        var license = await queries.GetLicenseAsync(id, scope, cancellationToken);

        // A consulta ja aplica o escopo: o que esta fora dele responde 404.
        return license ?? throw new NotFoundException("Licenca", id);
    }

    public async Task<Guid> CreateAsync(
        CreateLicenseCommand command,
        CancellationToken cancellationToken = default)
    {
        var unit = await organizationalUnits.GetAsync(command.OrganizationalUnitId, cancellationToken)
            ?? throw new NotFoundException("Unidade organizacional", command.OrganizationalUnitId);

        await EnsureCanWriteAsync(unit.Id, cancellationToken);

        if (await assets.CodeExistsAsync(command.Code, cancellationToken))
        {
            throw new ConflictException($"Ja existe um ativo com o codigo '{command.Code}'.");
        }

        var license = new LicenseAsset(
            Guid.NewGuid(),
            unit.TenantId,
            unit,
            command.Name,
            command.Code,
            command.Manufacturer,
            command.Product,
            command.ContractedQuantity,
            new Money(command.UnitPrice, command.Currency));

        license.DefinePlan(command.Plan, command.BillingType, currentUser.UserId);
        license.Describe(command.Description, currentUser.UserId);
        license.DefineValidity(command.StartDate, command.RenewalDate, currentUser.UserId);

        await LinkOptionalRelationsAsync(license, command, cancellationToken);

        assets.Add(license);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return license.Id;
    }

    /// <summary>
    /// Aplica um reajuste. O comando traz apenas o valor novo: o valor anterior
    /// vem do estado persistido, e diferenca, percentual e impacto anual sao
    /// calculados pelo dominio.
    /// </summary>
    public async Task<PriceAdjustmentResultDto> ApplyPriceAdjustmentAsync(
        Guid licenseId,
        ApplyPriceAdjustmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var license = await assets.GetLicenseAsync(licenseId, cancellationToken)
            ?? throw new NotFoundException("Licenca", licenseId);

        await EnsureCanWriteAsync(license.OrganizationalUnitId, cancellationToken);

        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Usuario nao identificado para registrar o reajuste.");

        /*
         * A aprovacao e exigida de quem nao tem a permissao de aprovar mudanca
         * financeira: um analista pode registrar o reajuste, desde que aponte
         * quem autorizou.
         */
        if (!currentUser.HasPermission(Permissions.AssetsApproveFinancialChange) &&
            command.ApprovedByUserId is null)
        {
            throw new ForbiddenException(
                "Reajustes registrados por este perfil exigem a indicacao do aprovador.");
        }

        if (command.ApprovedByUserId is { } approverId &&
            await users.GetAsync(approverId, cancellationToken) is null)
        {
            throw new NotFoundException("Usuario aprovador", approverId);
        }

        var adjustment = license.ApplyPriceAdjustment(
            new Money(command.NewMonthlyAmount, license.MonthlyAmount.Currency),
            command.EffectiveDate,
            command.Reason,
            command.IndexApplied,
            userId,
            command.ApprovedByUserId);

        priceAdjustments.Add(adjustment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new PriceAdjustmentResultDto(
            adjustment.Id,
            adjustment.PreviousAmount.Amount,
            adjustment.NewAmount.Amount,
            adjustment.AbsoluteDifference,
            adjustment.PercentageDifference.Value,
            adjustment.AnnualImpact,
            adjustment.NewAmount.Currency,
            adjustment.EffectiveDate,
            adjustment.Reason,
            adjustment.IndexApplied,
            adjustment.ExceedsThreshold(DefaultAdjustmentAlertThreshold));
    }

    public async Task<QuantityChangeResultDto> ChangeQuantityAsync(
        Guid licenseId,
        ChangeLicenseQuantityCommand command,
        CancellationToken cancellationToken = default)
    {
        var license = await assets.GetLicenseAsync(licenseId, cancellationToken)
            ?? throw new NotFoundException("Licenca", licenseId);

        await EnsureCanWriteAsync(license.OrganizationalUnitId, cancellationToken);

        var previousQuantity = license.ContractedQuantity;
        var previousMonthly = license.MonthlyAmount;

        var impact = license.ChangeContractedQuantity(
            command.NewQuantity,
            command.Reason,
            currentUser.UserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new QuantityChangeResultDto(
            license.Id,
            previousQuantity,
            license.ContractedQuantity,
            previousMonthly.Amount,
            license.MonthlyAmount.Amount,
            impact.Amount,
            impact.ToAnnual().Amount,
            impact.Currency);
    }

    public async Task UpdateUsageAsync(
        Guid licenseId,
        UpdateLicenseUsageCommand command,
        CancellationToken cancellationToken = default)
    {
        var license = await assets.GetLicenseAsync(licenseId, cancellationToken)
            ?? throw new NotFoundException("Licenca", licenseId);

        await EnsureCanWriteAsync(license.OrganizationalUnitId, cancellationToken);

        license.UpdateUsage(command.UsedQuantity, currentUser.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Calcula o impacto de um reajuste sem persistir nada.</summary>
    public static SimulationResultDto Simulate(SimulateAdjustmentQuery query)
    {
        var current = new Money(query.CurrentAmount, query.Currency);
        var proposed = new Money(query.NewAmount, query.Currency);

        var (difference, percentage, annualImpact) = PriceAdjustment.Simulate(current, proposed);

        return new SimulationResultDto(
            current.Amount,
            proposed.Amount,
            difference,
            percentage.Value,
            difference,
            annualImpact,
            current.Currency);
    }

    private async Task LinkOptionalRelationsAsync(
        LicenseAsset license,
        CreateLicenseCommand command,
        CancellationToken cancellationToken)
    {
        if (command.SupplierId is { } supplierId)
        {
            var supplier = await suppliers.GetAsync(supplierId, cancellationToken)
                ?? throw new NotFoundException("Fornecedor", supplierId);

            license.LinkToSupplier(supplier, currentUser.UserId);
        }

        if (command.ContractId is { } contractId)
        {
            var contract = await contracts.GetAsync(contractId, cancellationToken)
                ?? throw new NotFoundException("Contrato", contractId);

            license.LinkToContract(contract, currentUser.UserId);
        }

        if (command.CostCenterId is { } costCenterId)
        {
            var costCenter = await costCenters.GetAsync(costCenterId, cancellationToken)
                ?? throw new NotFoundException("Centro de custo", costCenterId);

            license.AssignCostCenter(costCenter, currentUser.UserId);
        }

        var technical = command.TechnicalResponsibleUserId is { } technicalId
            ? await users.GetAsync(technicalId, cancellationToken)
                ?? throw new NotFoundException("Responsavel tecnico", technicalId)
            : null;

        var financial = command.FinancialResponsibleUserId is { } financialId
            ? await users.GetAsync(financialId, cancellationToken)
                ?? throw new NotFoundException("Responsavel financeiro", financialId)
            : null;

        if (technical is not null || financial is not null)
        {
            license.AssignResponsibles(technical, financial, currentUser.UserId);
        }
    }

    /// <summary>
    /// Corrige o cadastro. Nao mexe em valor, quantidade nem situacao.
    /// </summary>
    public async Task UpdateAsync(
        Guid id,
        UpdateLicenseCommand command,
        CancellationToken cancellationToken = default)
    {
        var license = await assets.GetLicenseAsync(id, cancellationToken)
            ?? throw new NotFoundException("Licenca", id);

        await EnsureCanWriteAsync(license.OrganizationalUnitId, cancellationToken);

        var newCode = command.Code.Trim().ToUpperInvariant();

        if (newCode != license.Code && await assets.CodeExistsAsync(newCode, cancellationToken))
        {
            throw new ConflictException($"Ja existe um ativo com o codigo '{newCode}'.");
        }

        license.Rename(command.Name, command.Code, currentUser.UserId);
        license.CorrectProduct(command.Manufacturer, command.Product, currentUser.UserId);
        license.DefinePlan(command.Plan, command.BillingType, currentUser.UserId);
        license.Describe(command.Description, currentUser.UserId);
        license.DefineValidity(command.StartDate, command.RenewalDate, currentUser.UserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Cancela a licenca, preservando o registro e todo o historico.
    ///
    /// E o caminho correto quando a licenca existiu de fato — e o que a
    /// resposta 409 da exclusao orienta a fazer.
    /// </summary>
    public async Task CancelAsync(
        Guid id,
        CancelLicenseCommand command,
        CancellationToken cancellationToken = default)
    {
        var license = await assets.GetLicenseAsync(id, cancellationToken)
            ?? throw new NotFoundException("Licenca", id);

        await EnsureCanWriteAsync(license.OrganizationalUnitId, cancellationToken);

        license.ChangeStatus(AssetStatus.Cancelado, command.Reason, currentUser.UserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Remove a licenca em definitivo.
    ///
    /// Difere de desativar: a desativacao encerra algo que existiu e preserva a
    /// historia; a exclusao desfaz um cadastro equivocado. Por isso so e
    /// permitida enquanto o registro nao tiver consequencia financeira — sem
    /// reajustes e sem lancamentos de custo.
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var license = await assets.GetLicenseAsync(id, cancellationToken)
            ?? throw new NotFoundException("Licenca", id);

        await EnsureCanWriteAsync(license.OrganizationalUnitId, cancellationToken);

        if (!license.CanBeDeleted || await assets.HasCostEntriesAsync(id, cancellationToken))
        {
            throw new ConflictException(
                "Esta licenca ja possui historico financeiro e nao pode ser excluida. " +
                "Para encerrar o uso, altere a situacao para cancelada.");
        }

        assets.Remove(license);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCanWriteAsync(Guid organizationalUnitId, CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.GetAsync(cancellationToken);

        if (!scope.CanAccess(organizationalUnitId))
        {
            throw new ForbiddenException(
                "Voce nao possui acesso a unidade organizacional deste ativo.");
        }
    }
}
