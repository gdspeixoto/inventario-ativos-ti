using ControleAtivos.Application.Abstractions;
using ControleAtivos.Application.Common;
using ControleAtivos.Application.Features.Contracts;
using ControleAtivos.Domain.Entities;
using ControleAtivos.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ControleAtivos.Persistence.Queries;

public sealed class ContractQueries(ControleAtivosDbContext context) : IContractQueries
{
    public async Task<PagedResult<ContractListItemDto>> ListAsync(
        ListContractsQuery query,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default)
    {
        var contracts = WithinScope(context.Contracts.AsNoTracking(), scope);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            contracts = contracts.Where(c =>
                EF.Functions.ILike(c.Number, $"%{term}%") ||
                EF.Functions.ILike(c.Name, $"%{term}%") ||
                EF.Functions.ILike(c.Supplier.Name, $"%{term}%"));
        }

        if (query.Status is { } status)
        {
            contracts = contracts.Where(c => c.Status == status);
        }

        if (query.Category is { } category)
        {
            contracts = contracts.Where(c => c.Category == category);
        }

        if (query.SupplierId is { } supplierId)
        {
            contracts = contracts.Where(c => c.SupplierId == supplierId);
        }

        if (query.OrganizationalUnitId is { } unitId)
        {
            contracts = contracts.Where(c => c.OrganizationalUnitId == unitId);
        }

        if (query.ExpiringWithinDays is { } days)
        {
            var limit = today.AddDays(days);
            contracts = contracts.Where(c => c.EndDate >= today && c.EndDate <= limit);
        }

        var total = await contracts.LongCountAsync(cancellationToken);

        if (total == 0)
        {
            return PagedResult<ContractListItemDto>.Empty(query.Page, query.PageSize);
        }

        var items = await ApplySort(contracts, query)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(c => new ContractListItemDto(
                c.Id,
                c.Number,
                c.Name,
                c.Category,
                c.Status,
                c.SupplierId,
                c.Supplier.Name,
                c.OrganizationalUnit.Name,
                c.MonthlyAmount.Amount,
                c.MonthlyAmount.Amount * 12m,
                c.MonthlyAmount.Currency,
                c.StartDate,
                c.EndDate,
                c.EndDate.DayNumber - today.DayNumber,
                c.AdjustmentIndex,
                c.NextAdjustmentDate,
                c.Assets.Count,
                c.InternalResponsible != null ? c.InternalResponsible.DisplayName : null))
            .ToListAsync(cancellationToken);

        return new PagedResult<ContractListItemDto>(items, query.Page, query.PageSize, total);
    }

    public Task<ContractDetailDto?> GetAsync(
        Guid id,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return WithinScope(context.Contracts.AsNoTracking(), scope)
            .Where(c => c.Id == id)
            .Select(c => new ContractDetailDto(
                c.Id,
                c.Number,
                c.Name,
                c.Description,
                c.Category,
                c.Status,
                c.SupplierId,
                c.Supplier.Name,
                c.OrganizationalUnitId,
                c.OrganizationalUnit.Name,
                c.OrganizationalUnit.Path,
                c.MonthlyAmount.Amount,
                c.MonthlyAmount.Amount * 12m,
                c.MonthlyAmount.Currency,
                c.StartDate,
                c.EndDate,
                c.EndDate.DayNumber - today.DayNumber,
                c.AdjustmentIndex,
                c.AdjustmentPeriodicity,
                c.LastAdjustmentDate,
                c.NextAdjustmentDate,
                c.InternalResponsibleUserId,
                c.InternalResponsible != null ? c.InternalResponsible.DisplayName : null,
                c.Assets
                    .Select(a => new LinkedAssetDto(
                        a.Id,
                        a.Name,
                        a.Code,
                        a.Kind,
                        a.Status,
                        a.MonthlyAmount.Amount))
                    .ToList(),
                c.CreatedAt,
                c.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static IQueryable<Contract> WithinScope(
        IQueryable<Contract> source,
        IOrganizationalScope scope)
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
        return source.Where(c => unitIds.Contains(c.OrganizationalUnitId));
    }

    private static IQueryable<Contract> ApplySort(
        IQueryable<Contract> source,
        ListContractsQuery query) => (query.SortBy, query.Descending) switch
        {
            (ContractSortField.Number, false) => source.OrderBy(c => c.Number),
            (ContractSortField.Number, true) => source.OrderByDescending(c => c.Number),
            (ContractSortField.Name, false) => source.OrderBy(c => c.Name),
            (ContractSortField.Name, true) => source.OrderByDescending(c => c.Name),
            (ContractSortField.MonthlyAmount, false) => source.OrderBy(c => c.MonthlyAmount.Amount),
            (ContractSortField.MonthlyAmount, true) => source.OrderByDescending(c => c.MonthlyAmount.Amount),
            (_, true) => source.OrderByDescending(c => c.EndDate),
            // Padrao: o que vence antes aparece primeiro.
            _ => source.OrderBy(c => c.EndDate),
        };
}

public sealed class SupplierQueries(ControleAtivosDbContext context) : ISupplierQueries
{
    public async Task<PagedResult<Application.Features.Suppliers.SupplierListItemDto>> ListAsync(
        Application.Features.Suppliers.ListSuppliersQuery query,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default)
    {
        var suppliers = context.Suppliers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            suppliers = suppliers.Where(s =>
                EF.Functions.ILike(s.Name, $"%{term}%") ||
                (s.DocumentNumber != null && EF.Functions.ILike(s.DocumentNumber, $"%{term}%")));
        }

        if (query.Status is { } status)
        {
            suppliers = suppliers.Where(s => s.Status == status);
        }

        if (query.Category is { } category)
        {
            suppliers = suppliers.Where(s => s.Category == category);
        }

        var total = await suppliers.LongCountAsync(cancellationToken);

        if (total == 0)
        {
            return PagedResult<Application.Features.Suppliers.SupplierListItemDto>
                .Empty(query.Page, query.PageSize);
        }

        /*
         * Os totais financeiros por fornecedor respeitam o escopo do usuario:
         * quem enxerga apenas uma area ve o gasto daquela area com o fornecedor,
         * nao o gasto institucional inteiro.
         */
        var scopedUnits = scope.HasGlobalScope ? null : scope.UnitIds.ToList();

        var items = await suppliers
            .OrderBy(s => s.Name)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(s => new Application.Features.Suppliers.SupplierListItemDto(
                s.Id,
                s.Name,
                s.DocumentNumber,
                s.Category,
                s.Status,
                s.MainContactName,
                s.Email,
                s.Phone,
                context.Contracts.Count(c => c.SupplierId == s.Id
                    && c.Status == Domain.Enums.ContractStatus.Ativo
                    && (scopedUnits == null || scopedUnits.Contains(c.OrganizationalUnitId))),
                context.Assets.Count(a => a.SupplierId == s.Id
                    && (scopedUnits == null || scopedUnits.Contains(a.OrganizationalUnitId))),
                context.Assets
                    .Where(a => a.SupplierId == s.Id
                        && a.Status == Domain.Enums.AssetStatus.Ativo
                        && (scopedUnits == null || scopedUnits.Contains(a.OrganizationalUnitId)))
                    .Sum(a => (decimal?)a.MonthlyAmount.Amount) ?? 0m,
                (context.Assets
                    .Where(a => a.SupplierId == s.Id
                        && a.Status == Domain.Enums.AssetStatus.Ativo
                        && (scopedUnits == null || scopedUnits.Contains(a.OrganizationalUnitId)))
                    .Sum(a => (decimal?)a.MonthlyAmount.Amount) ?? 0m) * 12m,
                "BRL"))
            .ToListAsync(cancellationToken);

        return new PagedResult<Application.Features.Suppliers.SupplierListItemDto>(
            items,
            query.Page,
            query.PageSize,
            total);
    }

    public Task<Application.Features.Suppliers.SupplierDetailDto?> GetAsync(
        Guid id,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default)
    {
        var scopedUnits = scope.HasGlobalScope ? null : scope.UnitIds.ToList();

        return context.Suppliers
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new Application.Features.Suppliers.SupplierDetailDto(
                s.Id,
                s.Name,
                s.DocumentNumber,
                s.Category,
                s.Status,
                s.MainContactName,
                s.Email,
                s.Phone,
                s.Website,
                s.SlaDescription,
                context.Contracts.Count(c => c.SupplierId == s.Id
                    && c.Status == Domain.Enums.ContractStatus.Ativo
                    && (scopedUnits == null || scopedUnits.Contains(c.OrganizationalUnitId))),
                context.Assets.Count(a => a.SupplierId == s.Id
                    && (scopedUnits == null || scopedUnits.Contains(a.OrganizationalUnitId))),
                context.Assets
                    .Where(a => a.SupplierId == s.Id
                        && a.Status == Domain.Enums.AssetStatus.Ativo
                        && (scopedUnits == null || scopedUnits.Contains(a.OrganizationalUnitId)))
                    .Sum(a => (decimal?)a.MonthlyAmount.Amount) ?? 0m,
                (context.Assets
                    .Where(a => a.SupplierId == s.Id
                        && a.Status == Domain.Enums.AssetStatus.Ativo
                        && (scopedUnits == null || scopedUnits.Contains(a.OrganizationalUnitId)))
                    .Sum(a => (decimal?)a.MonthlyAmount.Amount) ?? 0m) * 12m,
                "BRL",
                context.Contracts
                    .Where(c => c.SupplierId == s.Id
                        && (scopedUnits == null || scopedUnits.Contains(c.OrganizationalUnitId)))
                    .OrderBy(c => c.EndDate)
                    .Select(c => new Application.Features.Suppliers.SupplierContractDto(
                        c.Id,
                        c.Number,
                        c.Name,
                        c.Status,
                        c.MonthlyAmount.Amount,
                        c.EndDate))
                    .ToList(),
                s.CreatedAt,
                s.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
