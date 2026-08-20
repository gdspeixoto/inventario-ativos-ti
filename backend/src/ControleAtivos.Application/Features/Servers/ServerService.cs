using ControleAtivos.Application.Abstractions;
using ControleAtivos.Application.Common;
using ControleAtivos.Domain.Entities;
using ControleAtivos.Domain.ValueObjects;

namespace ControleAtivos.Application.Features.Servers;

/// <summary>Casos de uso de servidores.</summary>
public sealed class ServerService(
    IAssetQueries queries,
    IAssetRepository assets,
    IOrganizationalUnitRepository organizationalUnits,
    ISupplierRepository suppliers,
    IContractRepository contracts,
    ICostCenterRepository costCenters,
    IUserRepository users,
    IOrganizationalScopeResolver scopeResolver,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
{
    public async Task<PagedResult<ServerListItemDto>> ListAsync(
        ListServersQuery query,
        CancellationToken cancellationToken = default)
    {
        var scope = await scopeResolver.GetAsync(cancellationToken);

        if (scope.IsEmpty)
        {
            return PagedResult<ServerListItemDto>.Empty(query.Page, query.PageSize);
        }

        return await queries.ListServersAsync(query, scope, cancellationToken);
    }

    public async Task<ServerDetailDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scope = await scopeResolver.GetAsync(cancellationToken);

        return await queries.GetServerAsync(id, scope, cancellationToken)
            ?? throw new NotFoundException("Servidor", id);
    }

    public async Task<Guid> CreateAsync(
        CreateServerCommand command,
        CancellationToken cancellationToken = default)
    {
        var unit = await organizationalUnits.GetAsync(command.OrganizationalUnitId, cancellationToken)
            ?? throw new NotFoundException("Unidade organizacional", command.OrganizationalUnitId);

        await EnsureCanWriteAsync(unit.Id, cancellationToken);

        if (await assets.CodeExistsAsync(command.Code, cancellationToken))
        {
            throw new ConflictException($"Ja existe um ativo com o codigo '{command.Code}'.");
        }

        var server = new ServerAsset(
            Guid.NewGuid(),
            unit.TenantId,
            unit,
            command.Name,
            command.Code,
            command.Hostname,
            command.ServerType,
            command.Environment,
            new Money(command.InfrastructureMonthlyCost, command.Currency));

        server.Describe(command.Description, currentUser.UserId);

        server.DefinePlatform(
            command.OperatingSystem,
            command.OperatingSystemVersion,
            command.Provider,
            command.RegionOrDatacenter,
            command.PrimaryIp,
            currentUser.UserId);

        if (command.CpuCores is not null || command.MemoryGb is not null || command.StorageGb is not null)
        {
            server.Resize(
                command.CpuCores,
                command.MemoryGb,
                command.StorageGb,
                "Configuracao inicial no cadastro",
                currentUser.UserId);
        }

        await LinkOptionalRelationsAsync(server, command, cancellationToken);

        assets.Add(server);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return server.Id;
    }

    public async Task<ServerCostChangeResultDto> UpdateCostsAsync(
        Guid serverId,
        UpdateServerCostsCommand command,
        CancellationToken cancellationToken = default)
    {
        var server = await assets.GetServerAsync(serverId, cancellationToken)
            ?? throw new NotFoundException("Servidor", serverId);

        await EnsureCanWriteAsync(server.OrganizationalUnitId, cancellationToken);

        var previousTotal = server.MonthlyAmount;

        var impact = server.UpdateCostBreakdown(
            new Money(command.InfrastructureMonthlyCost, command.Currency),
            new Money(command.LicenseMonthlyCost, command.Currency),
            new Money(command.SupportMonthlyCost, command.Currency),
            new Money(command.BackupMonthlyCost, command.Currency),
            command.Reason,
            currentUser.UserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ServerCostChangeResultDto(
            server.Id,
            previousTotal.Amount,
            server.MonthlyAmount.Amount,
            impact.Amount,
            impact.ToAnnual().Amount,
            impact.Currency);
    }

    public async Task ResizeAsync(
        Guid serverId,
        ResizeServerCommand command,
        CancellationToken cancellationToken = default)
    {
        var server = await assets.GetServerAsync(serverId, cancellationToken)
            ?? throw new NotFoundException("Servidor", serverId);

        await EnsureCanWriteAsync(server.OrganizationalUnitId, cancellationToken);

        server.Resize(
            command.CpuCores,
            command.MemoryGb,
            command.StorageGb,
            command.Reason,
            currentUser.UserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DecommissionAsync(
        Guid serverId,
        DecommissionServerCommand command,
        CancellationToken cancellationToken = default)
    {
        var server = await assets.GetServerAsync(serverId, cancellationToken)
            ?? throw new NotFoundException("Servidor", serverId);

        await EnsureCanWriteAsync(server.OrganizationalUnitId, cancellationToken);

        server.Decommission(command.Reason, currentUser.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task LinkOptionalRelationsAsync(
        ServerAsset server,
        CreateServerCommand command,
        CancellationToken cancellationToken)
    {
        if (command.SupplierId is { } supplierId)
        {
            var supplier = await suppliers.GetAsync(supplierId, cancellationToken)
                ?? throw new NotFoundException("Fornecedor", supplierId);

            server.LinkToSupplier(supplier, currentUser.UserId);
        }

        if (command.ContractId is { } contractId)
        {
            var contract = await contracts.GetAsync(contractId, cancellationToken)
                ?? throw new NotFoundException("Contrato", contractId);

            server.LinkToContract(contract, currentUser.UserId);
        }

        if (command.CostCenterId is { } costCenterId)
        {
            var costCenter = await costCenters.GetAsync(costCenterId, cancellationToken)
                ?? throw new NotFoundException("Centro de custo", costCenterId);

            server.AssignCostCenter(costCenter, currentUser.UserId);
        }

        if (command.TechnicalResponsibleUserId is { } technicalId)
        {
            var technical = await users.GetAsync(technicalId, cancellationToken)
                ?? throw new NotFoundException("Responsavel tecnico", technicalId);

            server.AssignResponsibles(technical, null, currentUser.UserId);
        }
    }

    /// <summary>Corrige o cadastro. Nao altera custos nem recursos.</summary>
    public async Task UpdateAsync(
        Guid id,
        UpdateServerCommand command,
        CancellationToken cancellationToken = default)
    {
        var server = await assets.GetServerAsync(id, cancellationToken)
            ?? throw new NotFoundException("Servidor", id);

        await EnsureCanWriteAsync(server.OrganizationalUnitId, cancellationToken);

        var newCode = command.Code.Trim().ToUpperInvariant();

        if (newCode != server.Code && await assets.CodeExistsAsync(newCode, cancellationToken))
        {
            throw new ConflictException($"Ja existe um ativo com o codigo '{newCode}'.");
        }

        server.Rename(command.Name, command.Code, currentUser.UserId);
        server.CorrectIdentification(
            command.Hostname, command.ServerType, command.Environment, currentUser.UserId);
        server.DefinePlatform(
            command.OperatingSystem,
            command.OperatingSystemVersion,
            command.Provider,
            command.RegionOrDatacenter,
            command.PrimaryIp,
            currentUser.UserId);
        server.Describe(command.Description, currentUser.UserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Remove o servidor em definitivo.
    ///
    /// Nao confundir com desativar: desativar registra que a maquina saiu de
    /// operacao — fato que precisa constar da historia. A exclusao apaga um
    /// cadastro que nao deveria existir, e por isso exige que nao haja custo
    /// registrado.
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var server = await assets.GetServerAsync(id, cancellationToken)
            ?? throw new NotFoundException("Servidor", id);

        await EnsureCanWriteAsync(server.OrganizationalUnitId, cancellationToken);

        if (!server.CanBeDeleted || await assets.HasCostEntriesAsync(id, cancellationToken))
        {
            throw new ConflictException(
                "Este servidor ja possui historico financeiro e nao pode ser excluido. " +
                "Para registrar que saiu de operacao, use a desativacao.");
        }

        assets.Remove(server);
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
