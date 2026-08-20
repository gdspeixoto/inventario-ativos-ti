using ControleAtivos.Application.Abstractions;
using ControleAtivos.Domain.Entities;
using ControleAtivos.Domain.Enums;
using ControleAtivos.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ControleAtivos.Persistence.Repositories;

/// <summary>
/// Repositorios de escrita. O filtro global de tenant do <c>DbContext</c> ja
/// atua em todas as consultas abaixo — nao ha <c>IgnoreQueryFilters</c> aqui.
/// </summary>
public sealed class AssetRepository(ControleAtivosDbContext context) : IAssetRepository
{
    public Task<LicenseAsset?> GetLicenseAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Licenses.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public Task<ServerAsset?> GetServerAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Servers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<Asset?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Assets.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default) =>
        context.Assets.AnyAsync(a => a.Code == code.ToUpperInvariant(), cancellationToken);

    public void Add(Asset asset) => context.Assets.Add(asset);

    public void Remove(Asset asset) => context.Assets.Remove(asset);

    public Task<bool> HasCostEntriesAsync(Guid assetId, CancellationToken cancellationToken = default) =>
        context.CostEntries.AnyAsync(e => e.AssetId == assetId, cancellationToken);
}

public sealed class OrganizationalUnitRepository(ControleAtivosDbContext context)
    : IOrganizationalUnitRepository
{
    public Task<OrganizationalUnit?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.OrganizationalUnits.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<OrganizationalUnit?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default) =>
        context.OrganizationalUnits
            .FirstOrDefaultAsync(u => u.Code == code.ToUpperInvariant(), cancellationToken);

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default) =>
        context.OrganizationalUnits
            .AnyAsync(u => u.Code == code.ToUpperInvariant(), cancellationToken);

    public void Add(OrganizationalUnit unit) => context.OrganizationalUnits.Add(unit);

    public void Remove(OrganizationalUnit unit) => context.OrganizationalUnits.Remove(unit);

    /*
     * Cada dependencia vira uma frase explicando o impedimento. Traduzir a
     * restricao de chave estrangeira em linguagem de negocio e o que permite ao
     * usuario saber o que fazer a seguir — mover os ativos, encerrar os
     * vinculos — em vez de apenas descobrir que "nao deu".
     */
    public async Task<IReadOnlyList<string>> GetDeletionBlockersAsync(
        Guid unitId,
        CancellationToken cancellationToken = default)
    {
        var blockers = new List<string>();

        var children = await context.OrganizationalUnits
            .CountAsync(u => u.ParentId == unitId, cancellationToken);

        if (children > 0)
        {
            blockers.Add($"{children} unidade(s) subordinada(s)");
        }

        var assets = await context.Assets
            .CountAsync(a => a.OrganizationalUnitId == unitId, cancellationToken);

        if (assets > 0)
        {
            blockers.Add($"{assets} ativo(s) vinculado(s)");
        }

        var contracts = await context.Contracts
            .CountAsync(c => c.OrganizationalUnitId == unitId, cancellationToken);

        if (contracts > 0)
        {
            blockers.Add($"{contracts} contrato(s) vinculado(s)");
        }

        var assignments = await context.ManagementAssignments
            .CountAsync(a => a.OrganizationalUnitId == unitId, cancellationToken);

        if (assignments > 0)
        {
            blockers.Add($"{assignments} vinculo(s) de gestao");
        }

        var costCenters = await context.CostCenters
            .CountAsync(c => c.OrganizationalUnitId == unitId, cancellationToken);

        if (costCenters > 0)
        {
            blockers.Add($"{costCenters} centro(s) de custo");
        }

        var entries = await context.CostEntries
            .CountAsync(e => e.OrganizationalUnitId == unitId, cancellationToken);

        if (entries > 0)
        {
            blockers.Add($"{entries} lancamento(s) de custo");
        }

        return blockers;
    }
}

public sealed class UserRepository(ControleAtivosDbContext context) : IUserRepository
{
    public Task<AppUser?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<AppUser?> GetByExternalSubjectAsync(
        string subject,
        CancellationToken cancellationToken = default) =>
        context.Users.FirstOrDefaultAsync(u => u.ExternalSubject == subject, cancellationToken);

    public void Add(AppUser user) => context.Users.Add(user);

    public async Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName })
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);
    }
}

public sealed class SupplierRepository(ControleAtivosDbContext context) : ISupplierRepository
{
    public Task<Supplier?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Suppliers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public void Add(Supplier supplier) => context.Suppliers.Add(supplier);

    public void Remove(Supplier supplier) => context.Suppliers.Remove(supplier);

    public async Task<bool> HasLinkedRecordsAsync(
        Guid supplierId,
        CancellationToken cancellationToken = default) =>
        await context.Assets.AnyAsync(a => a.SupplierId == supplierId, cancellationToken)
        || await context.Contracts.AnyAsync(c => c.SupplierId == supplierId, cancellationToken)
        || await context.CostEntries.AnyAsync(e => e.SupplierId == supplierId, cancellationToken);
}

public sealed class ContractRepository(ControleAtivosDbContext context) : IContractRepository
{
    public Task<Contract?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Contracts.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> NumberExistsAsync(string number, CancellationToken cancellationToken = default) =>
        context.Contracts.AnyAsync(c => c.Number == number, cancellationToken);

    public void Add(Contract contract) => context.Contracts.Add(contract);

    public void Remove(Contract contract) => context.Contracts.Remove(contract);

    public async Task<bool> HasLinkedRecordsAsync(
        Guid contractId,
        CancellationToken cancellationToken = default) =>
        await context.Assets.AnyAsync(a => a.ContractId == contractId, cancellationToken)
        || await context.CostEntries.AnyAsync(e => e.ContractId == contractId, cancellationToken);
}

public sealed class CostCenterRepository(ControleAtivosDbContext context) : ICostCenterRepository
{
    public Task<CostCenter?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.CostCenters.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public void Add(CostCenter costCenter) => context.CostCenters.Add(costCenter);
}

public sealed class PriceAdjustmentRepository(ControleAtivosDbContext context)
    : IPriceAdjustmentRepository
{
    public void Add(PriceAdjustment adjustment) => context.PriceAdjustments.Add(adjustment);
}

public sealed class ManagementAssignmentRepository(ControleAtivosDbContext context)
    : IManagementAssignmentRepository
{
    public Task<ManagementAssignment?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.ManagementAssignments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<bool> HasActiveAssignmentAsync(
        Guid userId,
        Guid organizationalUnitId,
        ManagementRole role,
        CancellationToken cancellationToken = default) =>
        context.ManagementAssignments.AnyAsync(
            a => a.UserId == userId
                && a.OrganizationalUnitId == organizationalUnitId
                && a.Role == role
                && a.EndDate == null,
            cancellationToken);

    public void Add(ManagementAssignment assignment) => context.ManagementAssignments.Add(assignment);
}

public sealed class CostEntryRepository(ControleAtivosDbContext context) : ICostEntryRepository
{
    public void Add(CostEntry entry) => context.CostEntries.Add(entry);
}

public sealed class DocumentRepository(ControleAtivosDbContext context) : IDocumentRepository
{
    public Task<Document?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Document>> ListAsync(
        TimelineEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken = default) =>
        await context.Documents
            .AsNoTracking()
            .Where(d => d.EntityType == entityType && d.EntityId == entityId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

    public void Add(Document document) => context.Documents.Add(document);

    public void Remove(Document document) => context.Documents.Remove(document);
}
