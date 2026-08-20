using ControleAtivos.Application.Abstractions;
using ControleAtivos.Application.Common;
using ControleAtivos.Application.Features.Licenses;
using ControleAtivos.Application.Features.Servers;
using ControleAtivos.Domain.Entities;
using ControleAtivos.Domain.Enums;
using ControleAtivos.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ControleAtivos.Persistence.Queries;

/// <summary>
/// Consultas de leitura de ativos, projetadas direto para DTO.
///
/// O filtro de tenant vem do <c>DbContext</c>; o filtro de escopo organizacional
/// e aplicado aqui, em <see cref="WithinScope{T}"/>, e vale tanto para listagens
/// quanto para detalhes — um item fora do escopo responde 404, e nao 403, para
/// nao revelar sua existencia.
/// </summary>
public sealed class AssetQueries(ControleAtivosDbContext context) : IAssetQueries
{
    public async Task<PagedResult<LicenseListItemDto>> ListLicensesAsync(
        ListLicensesQuery query,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default)
    {
        var licenses = WithinScope(context.Licenses.AsNoTracking(), scope);

        licenses = ApplyLicenseFilters(licenses, query);

        var total = await licenses.LongCountAsync(cancellationToken);

        if (total == 0)
        {
            return PagedResult<LicenseListItemDto>.Empty(query.Page, query.PageSize);
        }

        var items = await ApplyLicenseSort(licenses, query)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(l => new LicenseListItemDto(
                l.Id,
                l.Name,
                l.Code,
                l.Manufacturer,
                l.Product,
                l.Plan,
                l.Status,
                l.BillingType,
                l.ContractedQuantity,
                l.UsedQuantity,
                l.ContractedQuantity - l.UsedQuantity,
                l.ContractedQuantity == 0
                    ? 0m
                    : Math.Round((decimal)l.UsedQuantity / l.ContractedQuantity * 100m, 2),
                l.UnitPrice.Amount,
                l.MonthlyAmount.Amount,
                l.MonthlyAmount.Amount * 12m,
                l.MonthlyAmount.Currency,
                l.Supplier != null ? l.Supplier.Name : null,
                l.OrganizationalUnit.Name,
                l.RenewalDate,
                l.LastAdjustmentDate,
                l.NextAdjustmentDate))
            .ToListAsync(cancellationToken);

        return new PagedResult<LicenseListItemDto>(items, query.Page, query.PageSize, total);
    }

    public Task<LicenseDetailDto?> GetLicenseAsync(
        Guid id,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default) =>
        WithinScope(context.Licenses.AsNoTracking(), scope)
            .Where(l => l.Id == id)
            .Select(l => new LicenseDetailDto(
                l.Id,
                l.Name,
                l.Code,
                l.Description,
                l.Manufacturer,
                l.Product,
                l.Plan,
                l.Status,
                l.BillingType,
                l.ContractedQuantity,
                l.UsedQuantity,
                l.ContractedQuantity - l.UsedQuantity,
                l.ContractedQuantity == 0
                    ? 0m
                    : Math.Round((decimal)l.UsedQuantity / l.ContractedQuantity * 100m, 2),
                l.UnitPrice.Amount,
                l.MonthlyAmount.Amount,
                l.MonthlyAmount.Amount * 12m,
                l.MonthlyAmount.Currency,
                l.OrganizationalUnitId,
                l.OrganizationalUnit.Name,
                l.OrganizationalUnit.Path,
                l.SupplierId,
                l.Supplier != null ? l.Supplier.Name : null,
                l.ContractId,
                l.Contract != null ? l.Contract.Number : null,
                l.CostCenterId,
                l.CostCenter != null ? l.CostCenter.Name : null,
                l.TechnicalResponsibleUserId,
                l.TechnicalResponsible != null ? l.TechnicalResponsible.DisplayName : null,
                l.FinancialResponsibleUserId,
                l.FinancialResponsible != null ? l.FinancialResponsible.DisplayName : null,
                l.StartDate,
                l.RenewalDate,
                l.LastAdjustmentDate,
                l.NextAdjustmentDate,
                l.CreatedAt,
                l.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<ServerListItemDto>> ListServersAsync(
        ListServersQuery query,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default)
    {
        var servers = WithinScope(context.Servers.AsNoTracking(), scope);

        servers = ApplyServerFilters(servers, query);

        var total = await servers.LongCountAsync(cancellationToken);

        if (total == 0)
        {
            return PagedResult<ServerListItemDto>.Empty(query.Page, query.PageSize);
        }

        var items = await ApplyServerSort(servers, query)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(s => new ServerListItemDto(
                s.Id,
                s.Name,
                s.Code,
                s.Hostname,
                s.ServerType,
                s.Environment,
                s.Status,
                s.OperatingSystem,
                s.Provider,
                s.CpuCores,
                s.MemoryGb,
                s.StorageGb,
                s.MonthlyAmount.Amount,
                s.MonthlyAmount.Amount * 12m,
                s.MonthlyAmount.Currency,
                s.TechnicalResponsible != null ? s.TechnicalResponsible.DisplayName : null,
                s.OrganizationalUnit.Name,
                s.LastBackupAt,
                s.TechnicalResponsibleUserId != null))
            .ToListAsync(cancellationToken);

        return new PagedResult<ServerListItemDto>(items, query.Page, query.PageSize, total);
    }

    public Task<ServerDetailDto?> GetServerAsync(
        Guid id,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default) =>
        WithinScope(context.Servers.AsNoTracking(), scope)
            .Where(s => s.Id == id)
            .Select(s => new ServerDetailDto(
                s.Id,
                s.Name,
                s.Code,
                s.Description,
                s.Hostname,
                s.ServerType,
                s.Environment,
                s.Status,
                s.OperatingSystem,
                s.OperatingSystemVersion,
                s.Provider,
                s.RegionOrDatacenter,
                s.PrimaryIp,
                s.CpuCores,
                s.MemoryGb,
                s.StorageGb,
                s.BackupPolicy,
                s.LastBackupAt,
                s.Sla,
                s.MaintenanceWindow,
                s.InfrastructureMonthlyCost.Amount,
                s.LicenseMonthlyCost.Amount,
                s.SupportMonthlyCost.Amount,
                s.BackupMonthlyCost.Amount,
                s.MonthlyAmount.Amount,
                s.MonthlyAmount.Amount * 12m,
                s.MonthlyAmount.Currency,
                s.OrganizationalUnitId,
                s.OrganizationalUnit.Name,
                s.OrganizationalUnit.Path,
                s.SupplierId,
                s.Supplier != null ? s.Supplier.Name : null,
                s.ContractId,
                s.Contract != null ? s.Contract.Number : null,
                s.CostCenterId,
                s.CostCenter != null ? s.CostCenter.Name : null,
                s.TechnicalResponsibleUserId,
                s.TechnicalResponsible != null ? s.TechnicalResponsible.DisplayName : null,
                s.CreatedAt,
                s.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Restringe a consulta as unidades acessiveis. Escopo global nao aplica
    /// filtro; escopo vazio elimina todos os resultados.
    /// </summary>
    private static IQueryable<T> WithinScope<T>(IQueryable<T> source, IOrganizationalScope scope)
        where T : Asset
    {
        if (scope.HasGlobalScope)
        {
            return source;
        }

        if (scope.IsEmpty)
        {
            return source.Where(_ => false);
        }

        var unitIds = scope.UnitIds.ToList();
        return source.Where(a => unitIds.Contains(a.OrganizationalUnitId));
    }

    private static IQueryable<LicenseAsset> ApplyLicenseFilters(
        IQueryable<LicenseAsset> source,
        ListLicensesQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            source = source.Where(l =>
                EF.Functions.ILike(l.Name, $"%{term}%") ||
                EF.Functions.ILike(l.Code, $"%{term}%") ||
                EF.Functions.ILike(l.Product, $"%{term}%") ||
                EF.Functions.ILike(l.Manufacturer, $"%{term}%"));
        }

        if (query.Status is { } status)
        {
            source = source.Where(l => l.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Manufacturer))
        {
            source = source.Where(l => l.Manufacturer == query.Manufacturer);
        }

        if (query.SupplierId is { } supplierId)
        {
            source = source.Where(l => l.SupplierId == supplierId);
        }

        if (query.OrganizationalUnitId is { } unitId)
        {
            source = source.Where(l => l.OrganizationalUnitId == unitId);
        }

        if (query.CostCenterId is { } costCenterId)
        {
            source = source.Where(l => l.CostCenterId == costCenterId);
        }

        if (query.RenewalWithinDays is { } days)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var limit = today.AddDays(days);
            source = source.Where(l => l.RenewalDate != null
                && l.RenewalDate >= today
                && l.RenewalDate <= limit);
        }

        // Utilizacao e derivada; o calculo precisa ir para o SQL.
        if (query.UtilizationBelow is { } below)
        {
            source = source.Where(l => l.ContractedQuantity > 0
                && (decimal)l.UsedQuantity / l.ContractedQuantity * 100m < below);
        }

        if (query.UtilizationAbove is { } above)
        {
            source = source.Where(l => l.ContractedQuantity > 0
                && (decimal)l.UsedQuantity / l.ContractedQuantity * 100m > above);
        }

        return source;
    }

    private static IQueryable<LicenseAsset> ApplyLicenseSort(
        IQueryable<LicenseAsset> source,
        ListLicensesQuery query) => (query.SortBy, query.Descending) switch
        {
            (LicenseSortField.MonthlyAmount, false) => source.OrderBy(l => l.MonthlyAmount.Amount),
            (LicenseSortField.MonthlyAmount, true) => source.OrderByDescending(l => l.MonthlyAmount.Amount),
            (LicenseSortField.RenewalDate, false) => source.OrderBy(l => l.RenewalDate),
            (LicenseSortField.RenewalDate, true) => source.OrderByDescending(l => l.RenewalDate),
            (LicenseSortField.Utilization, false) => source.OrderBy(l =>
                l.ContractedQuantity == 0 ? 0m : (decimal)l.UsedQuantity / l.ContractedQuantity),
            (LicenseSortField.Utilization, true) => source.OrderByDescending(l =>
                l.ContractedQuantity == 0 ? 0m : (decimal)l.UsedQuantity / l.ContractedQuantity),
            (LicenseSortField.CreatedAt, false) => source.OrderBy(l => l.CreatedAt),
            (LicenseSortField.CreatedAt, true) => source.OrderByDescending(l => l.CreatedAt),
            (_, true) => source.OrderByDescending(l => l.Name),
            _ => source.OrderBy(l => l.Name),
        };

    private static IQueryable<ServerAsset> ApplyServerFilters(
        IQueryable<ServerAsset> source,
        ListServersQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            source = source.Where(s =>
                EF.Functions.ILike(s.Name, $"%{term}%") ||
                EF.Functions.ILike(s.Code, $"%{term}%") ||
                EF.Functions.ILike(s.Hostname, $"%{term}%"));
        }

        if (query.Status is { } status)
        {
            source = source.Where(s => s.Status == status);
        }

        if (query.Environment is { } environment)
        {
            source = source.Where(s => s.Environment == environment);
        }

        if (query.ServerType is { } serverType)
        {
            source = source.Where(s => s.ServerType == serverType);
        }

        if (!string.IsNullOrWhiteSpace(query.Provider))
        {
            source = source.Where(s => s.Provider == query.Provider);
        }

        if (query.OrganizationalUnitId is { } unitId)
        {
            source = source.Where(s => s.OrganizationalUnitId == unitId);
        }

        if (query.WithoutResponsible == true)
        {
            source = source.Where(s => s.TechnicalResponsibleUserId == null);
        }

        if (query.WithoutBackupForDays is { } backupDays)
        {
            var limit = DateTimeOffset.UtcNow.AddDays(-backupDays);
            source = source.Where(s => s.LastBackupAt == null || s.LastBackupAt < limit);
        }

        if (query.MonthlyCostAbove is { } costAbove)
        {
            source = source.Where(s => s.MonthlyAmount.Amount > costAbove);
        }

        return source;
    }

    private static IQueryable<ServerAsset> ApplyServerSort(
        IQueryable<ServerAsset> source,
        ListServersQuery query) => (query.SortBy, query.Descending) switch
        {
            (ServerSortField.MonthlyAmount, false) => source.OrderBy(s => s.MonthlyAmount.Amount),
            (ServerSortField.MonthlyAmount, true) => source.OrderByDescending(s => s.MonthlyAmount.Amount),
            (ServerSortField.Environment, false) => source.OrderBy(s => s.Environment).ThenBy(s => s.Name),
            (ServerSortField.Environment, true) => source.OrderByDescending(s => s.Environment).ThenBy(s => s.Name),
            (ServerSortField.CreatedAt, false) => source.OrderBy(s => s.CreatedAt),
            (ServerSortField.CreatedAt, true) => source.OrderByDescending(s => s.CreatedAt),
            (_, true) => source.OrderByDescending(s => s.Name),
            _ => source.OrderBy(s => s.Name),
        };
}
