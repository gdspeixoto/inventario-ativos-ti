using ControleAtivos.Application.Abstractions;
using ControleAtivos.Application.Common;
using ControleAtivos.Application.Features.Licenses;
using ControleAtivos.Domain.Entities;
using ControleAtivos.Domain.ValueObjects;

namespace ControleAtivos.Application.Features.Contracts;

public sealed class ContractService(
    IContractQueries queries,
    IContractRepository contracts,
    ISupplierRepository suppliers,
    IOrganizationalUnitRepository organizationalUnits,
    IUserRepository users,
    IPriceAdjustmentRepository priceAdjustments,
    IOrganizationalScopeResolver scopeResolver,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
{
    public async Task<PagedResult<ContractListItemDto>> ListAsync(
        ListContractsQuery query,
        CancellationToken cancellationToken = default)
    {
        var scope = await scopeResolver.GetAsync(cancellationToken);

        return scope.IsEmpty
            ? PagedResult<ContractListItemDto>.Empty(query.Page, query.PageSize)
            : await queries.ListAsync(query, scope, cancellationToken);
    }

    public async Task<ContractDetailDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scope = await scopeResolver.GetAsync(cancellationToken);

        return await queries.GetAsync(id, scope, cancellationToken)
            ?? throw new NotFoundException("Contrato", id);
    }

    public async Task<Guid> CreateAsync(
        CreateContractCommand command,
        CancellationToken cancellationToken = default)
    {
        var unit = await organizationalUnits.GetAsync(command.OrganizationalUnitId, cancellationToken)
            ?? throw new NotFoundException("Unidade organizacional", command.OrganizationalUnitId);

        await EnsureCanWriteAsync(unit.Id, cancellationToken);

        if (await contracts.NumberExistsAsync(command.Number, cancellationToken))
        {
            throw new ConflictException($"Ja existe um contrato com o numero '{command.Number}'.");
        }

        var supplier = await suppliers.GetAsync(command.SupplierId, cancellationToken)
            ?? throw new NotFoundException("Fornecedor", command.SupplierId);

        var contract = new Contract(
            Guid.NewGuid(),
            unit.TenantId,
            supplier,
            unit,
            command.Number,
            command.Name,
            command.Category,
            command.StartDate,
            command.EndDate,
            new Money(command.MonthlyAmount, command.Currency));

        contract.DefineAdjustmentRules(
            command.AdjustmentIndex,
            command.AdjustmentPeriodicity,
            null,
            currentUser.UserId);

        if (command.InternalResponsibleUserId is { } responsibleId)
        {
            var responsible = await users.GetAsync(responsibleId, cancellationToken)
                ?? throw new NotFoundException("Responsavel interno", responsibleId);

            contract.AssignResponsible(responsible, currentUser.UserId);
        }

        contracts.Add(contract);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return contract.Id;
    }

    /// <summary>
    /// Prorroga a vigencia. Nao altera valores: um eventual reajuste e
    /// registrado separadamente, mantendo os dois fatos distinguiveis.
    /// </summary>
    public async Task RenewAsync(
        Guid contractId,
        RenewContractCommand command,
        CancellationToken cancellationToken = default)
    {
        var contract = await contracts.GetAsync(contractId, cancellationToken)
            ?? throw new NotFoundException("Contrato", contractId);

        await EnsureCanWriteAsync(contract.OrganizationalUnitId, cancellationToken);

        contract.Renew(command.NewEndDate, command.Reason, currentUser.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PriceAdjustmentResultDto> ApplyPriceAdjustmentAsync(
        Guid contractId,
        ApplyPriceAdjustmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var contract = await contracts.GetAsync(contractId, cancellationToken)
            ?? throw new NotFoundException("Contrato", contractId);

        await EnsureCanWriteAsync(contract.OrganizationalUnitId, cancellationToken);

        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Usuario nao identificado para registrar o reajuste.");

        if (!currentUser.HasPermission(Permissions.AssetsApproveFinancialChange) &&
            command.ApprovedByUserId is null)
        {
            throw new ForbiddenException(
                "Reajustes registrados por este perfil exigem a indicacao do aprovador.");
        }

        var adjustment = contract.ApplyPriceAdjustment(
            new Money(command.NewMonthlyAmount, contract.MonthlyAmount.Currency),
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
            adjustment.ExceedsThreshold(LicenseService.DefaultAdjustmentAlertThreshold));
    }

    public async Task TerminateAsync(
        Guid contractId,
        TerminateContractCommand command,
        CancellationToken cancellationToken = default)
    {
        var contract = await contracts.GetAsync(contractId, cancellationToken)
            ?? throw new NotFoundException("Contrato", contractId);

        await EnsureCanWriteAsync(contract.OrganizationalUnitId, cancellationToken);

        contract.Terminate(command.Status, command.Reason, currentUser.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCanWriteAsync(Guid organizationalUnitId, CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.GetAsync(cancellationToken);

        if (!scope.CanAccess(organizationalUnitId))
        {
            throw new ForbiddenException(
                "Voce nao possui acesso a unidade organizacional deste contrato.");
        }
    }
    /// <summary>
    /// Remove o contrato em definitivo.
    ///
    /// Encerrar e excluir nao se confundem: o encerramento registra o fim da
    /// vigencia de um contrato que valeu, e isso e informacao. A exclusao
    /// apaga um cadastro equivocado, e por isso exige que nenhum ativo ou
    /// lancamento dependa dele.
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contract = await contracts.GetAsync(id, cancellationToken)
            ?? throw new NotFoundException("Contrato", id);

        await EnsureCanWriteAsync(contract.OrganizationalUnitId, cancellationToken);

        if (await contracts.HasLinkedRecordsAsync(id, cancellationToken))
        {
            throw new ConflictException(
                "Este contrato possui ativos ou lancamentos vinculados e nao pode ser excluido. " +
                "Para registrar o fim da vigencia, use o encerramento.");
        }

        contracts.Remove(contract);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

}
