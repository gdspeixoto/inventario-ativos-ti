using ControleAtivos.Application.Abstractions;
using ControleAtivos.Application.Common;
using ControleAtivos.Application.Features.Costs;
using ControleAtivos.Domain.Entities;
using ControleAtivos.Domain.Enums;
using ControleAtivos.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ControleAtivos.Persistence.Queries;

public sealed class CostQueries(ControleAtivosDbContext context) : ICostQueries
{
    public async Task<PagedResult<PriceAdjustmentListItemDto>> ListAdjustmentsAsync(
        ListPriceAdjustmentsQuery query,
        decimal thresholdPercentage,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default)
    {
        var adjustments = WithinScope(context.PriceAdjustments.AsNoTracking(), scope);

        if (query.TargetType is { } targetType)
        {
            adjustments = adjustments.Where(p => p.TargetType == targetType);
        }

        if (query.TargetId is { } targetId)
        {
            adjustments = adjustments.Where(p => p.TargetId == targetId);
        }

        if (query.OrganizationalUnitId is { } unitId)
        {
            adjustments = adjustments.Where(p => p.OrganizationalUnitId == unitId);
        }

        if (query.From is { } from)
        {
            adjustments = adjustments.Where(p => p.EffectiveDate >= from);
        }

        if (query.To is { } to)
        {
            adjustments = adjustments.Where(p => p.EffectiveDate <= to);
        }

        if (query.OnlyIncreases == true)
        {
            adjustments = adjustments.Where(p => p.NewAmount.Amount > p.PreviousAmount.Amount);
        }

        if (query.OnlyAboveThreshold == true)
        {
            /*
             * O percentual e derivado, entao o filtro precisa reproduzir o
             * calculo em SQL. Valor anterior zerado nao tem percentual definido
             * e fica de fora.
             */
            adjustments = adjustments.Where(p => p.PreviousAmount.Amount > 0
                && Math.Abs((p.NewAmount.Amount - p.PreviousAmount.Amount)
                    / p.PreviousAmount.Amount * 100m) > thresholdPercentage);
        }

        var total = await adjustments.LongCountAsync(cancellationToken);

        if (total == 0)
        {
            return PagedResult<PriceAdjustmentListItemDto>.Empty(query.Page, query.PageSize);
        }

        var items = await adjustments
            .OrderByDescending(p => p.EffectiveDate)
            .ThenByDescending(p => p.CreatedAt)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(p => new PriceAdjustmentListItemDto(
                p.Id,
                p.TargetType,
                p.TargetId,
                p.TargetType == PriceAdjustmentTargetType.Contract
                    ? context.Contracts.Where(c => c.Id == p.TargetId)
                        .Select(c => c.Number + " - " + c.Name).FirstOrDefault() ?? "-"
                    : context.Assets.Where(a => a.Id == p.TargetId)
                        .Select(a => a.Name).FirstOrDefault() ?? "-",
                p.PreviousAmount.Amount,
                p.NewAmount.Amount,
                p.NewAmount.Amount - p.PreviousAmount.Amount,
                p.PreviousAmount.Amount == 0
                    ? 0m
                    : Math.Round(
                        (p.NewAmount.Amount - p.PreviousAmount.Amount) / p.PreviousAmount.Amount * 100m,
                        4),
                (p.NewAmount.Amount - p.PreviousAmount.Amount) * 12m,
                p.NewAmount.Currency,
                p.EffectiveDate,
                p.Reason,
                p.IndexApplied,
                context.Users.Where(u => u.Id == p.CreatedByUserId)
                    .Select(u => u.DisplayName).FirstOrDefault(),
                p.ApprovedByUserId == null
                    ? null
                    : context.Users.Where(u => u.Id == p.ApprovedByUserId)
                        .Select(u => u.DisplayName).FirstOrDefault(),
                context.OrganizationalUnits.Where(u => u.Id == p.OrganizationalUnitId)
                    .Select(u => u.Name).FirstOrDefault() ?? "-",
                p.PreviousAmount.Amount > 0
                    && Math.Abs((p.NewAmount.Amount - p.PreviousAmount.Amount)
                        / p.PreviousAmount.Amount * 100m) > thresholdPercentage))
            .ToListAsync(cancellationToken);

        return new PagedResult<PriceAdjustmentListItemDto>(items, query.Page, query.PageSize, total);
    }

    public async Task<CostSummaryDto> GetSummaryAsync(
        decimal thresholdPercentage,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default)
    {
        var assets = WithinAssetScope(context.Assets.AsNoTracking(), scope)
            .Where(a => a.Status == AssetStatus.Ativo);

        var monthlyCost = await assets.SumAsync(a => (decimal?)a.MonthlyAmount.Amount, cancellationToken)
            ?? 0m;

        var adjustments = WithinScope(context.PriceAdjustments.AsNoTracking(), scope);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var yearAgo = today.AddYears(-1);

        var monthAdjustments = await adjustments
            .Where(p => p.EffectiveDate >= monthStart)
            .Select(p => new
            {
                Difference = p.NewAmount.Amount - p.PreviousAmount.Amount,
                Percentage = p.PreviousAmount.Amount == 0
                    ? 0m
                    : (p.NewAmount.Amount - p.PreviousAmount.Amount) / p.PreviousAmount.Amount * 100m,
            })
            .ToListAsync(cancellationToken);

        var periodAdjustments = await adjustments
            .Where(p => p.EffectiveDate >= yearAgo)
            .Select(p => new
            {
                Difference = p.NewAmount.Amount - p.PreviousAmount.Amount,
                Percentage = p.PreviousAmount.Amount == 0
                    ? 0m
                    : (p.NewAmount.Amount - p.PreviousAmount.Amount) / p.PreviousAmount.Amount * 100m,
            })
            .ToListAsync(cancellationToken);

        // Ativo criado ha mais de um ano e nunca reajustado, ou reajustado ha mais de um ano.
        var stale = await assets
            .CountAsync(
                a => (a.LastAdjustmentDate == null && a.CreatedAt < DateTimeOffset.UtcNow.AddYears(-1))
                    || (a.LastAdjustmentDate != null && a.LastAdjustmentDate < yearAgo),
                cancellationToken);

        var byCategory = await assets
            .Select(a => new { a.Kind, Amount = a.MonthlyAmount.Amount })
            .GroupBy(a => a.Kind)
            .Select(g => new CostByGroupDto(
                g.Key.ToString(),
                null,
                g.Sum(a => a.Amount),
                g.Sum(a => a.Amount) * 12m,
                g.Count()))
            .ToListAsync(cancellationToken);

        /*
         * O agrupamento projeta o valor para uma coluna simples antes do
         * GroupBy. Agregar `MonthlyAmount.Amount` diretamente falha: `Money` e
         * um complex type e, depois do join com a navegacao, o provider nao
         * consegue traduzir a soma — o erro so aparece em execucao, ja como 500.
         */
        /*
         * O agrupamento por fornecedor soma primeiro e so depois busca o nome.
         * Agrupar por uma propriedade da navegacao (`a.Supplier.Name`) obriga o
         * provider a combinar join e agregacao sobre um complex type, o que ele
         * nao traduz — e a falha so aparece em execucao, como 500.
         */
        var supplierTotals = await assets
            .Where(a => a.SupplierId != null)
            .GroupBy(a => a.SupplierId)
            .Select(g => new
            {
                SupplierId = g.Key,
                MonthlyAmount = g.Sum(a => a.MonthlyAmount.Amount),
                ItemCount = g.Count(),
            })
            .OrderByDescending(g => g.MonthlyAmount)
            .Take(20)
            .ToListAsync(cancellationToken);

        var supplierIds = supplierTotals.Select(t => t.SupplierId).ToList();

        var supplierNames = await context.Suppliers
            .AsNoTracking()
            .Where(s => supplierIds.Contains(s.Id))
            .Select(s => new { s.Id, s.Name })
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        var bySupplier = supplierTotals
            .Select(t => new CostByGroupDto(
                supplierNames.GetValueOrDefault(t.SupplierId!.Value, "Sem fornecedor"),
                t.SupplierId,
                t.MonthlyAmount,
                t.MonthlyAmount * 12m,
                t.ItemCount))
            .ToList();

        var unitTotals = await assets
            .GroupBy(a => a.OrganizationalUnitId)
            .Select(g => new
            {
                UnitId = g.Key,
                MonthlyAmount = g.Sum(a => a.MonthlyAmount.Amount),
                ItemCount = g.Count(),
            })
            .OrderByDescending(g => g.MonthlyAmount)
            .Take(20)
            .ToListAsync(cancellationToken);

        var unitIds = unitTotals.Select(t => t.UnitId).ToList();

        var unitNames = await context.OrganizationalUnits
            .AsNoTracking()
            .Where(u => unitIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Name })
            .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

        var byUnit = unitTotals
            .Select(t => new CostByGroupDto(
                unitNames.GetValueOrDefault(t.UnitId, "Sem unidade"),
                t.UnitId,
                t.MonthlyAmount,
                t.MonthlyAmount * 12m,
                t.ItemCount))
            .ToList();

        return new CostSummaryDto(
            monthlyCost,
            monthlyCost * 12m,
            "BRL",
            monthAdjustments.Sum(a => a.Difference),
            periodAdjustments.Sum(a => a.Difference),
            periodAdjustments.Sum(a => a.Difference) * 12m,
            periodAdjustments.Count == 0 ? null : periodAdjustments.Max(a => a.Percentage),
            periodAdjustments.Count == 0 ? null : periodAdjustments.Max(a => a.Difference),
            stale,
            periodAdjustments.Count,
            byCategory,
            bySupplier,
            byUnit);
    }

    private static IQueryable<PriceAdjustment> WithinScope(
        IQueryable<PriceAdjustment> source,
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
        return source.Where(p => unitIds.Contains(p.OrganizationalUnitId));
    }

    public async Task<PagedResult<CostEntryDto>> ListEntriesAsync(
        ListCostEntriesQuery query,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default)
    {
        var entries = WithinEntryScope(context.CostEntries.AsNoTracking(), scope);

        if (query.OrganizationalUnitId is { } unitId)
        {
            entries = entries.Where(e => e.OrganizationalUnitId == unitId);
        }

        if (query.AssetId is { } assetId)
        {
            entries = entries.Where(e => e.AssetId == assetId);
        }

        if (query.ContractId is { } contractId)
        {
            entries = entries.Where(e => e.ContractId == contractId);
        }

        if (query.Category is { } category)
        {
            entries = entries.Where(e => e.Category == category);
        }

        if (query.Type is { } type)
        {
            entries = entries.Where(e => e.Type == type);
        }

        if (query.From is { } from)
        {
            entries = entries.Where(e => e.CompetenceMonth >= from);
        }

        if (query.To is { } to)
        {
            entries = entries.Where(e => e.CompetenceMonth <= to);
        }

        var total = await entries.LongCountAsync(cancellationToken);

        var page = await entries
            .OrderByDescending(e => e.CompetenceMonth)
            .ThenByDescending(e => e.CreatedAt)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(e => new
            {
                e.Id,
                e.OrganizationalUnitId,
                e.AssetId,
                AssetName = e.Asset != null ? e.Asset.Name : null,
                e.ContractId,
                ContractNumber = e.Contract != null ? e.Contract.Number : null,
                e.SupplierId,
                SupplierName = e.Supplier != null ? e.Supplier.Name : null,
                e.Type,
                e.Category,
                Amount = e.Amount.Amount,
                Currency = e.Amount.Currency,
                e.CompetenceMonth,
                e.EffectiveDate,
                e.Description,
                e.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        /*
         * O nome da unidade vem numa consulta a parte: `CostEntry` guarda
         * apenas o id, sem navegacao, e projetar por join dentro do GroupBy
         * fez o provider falhar em traducao em outras consultas deste arquivo.
         */
        var unitIds = page.Select(e => e.OrganizationalUnitId).Distinct().ToList();

        var unitNames = await context.OrganizationalUnits
            .AsNoTracking()
            .Where(u => unitIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Name })
            .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

        var items = page
            .Select(e => new CostEntryDto(
                e.Id,
                e.OrganizationalUnitId,
                unitNames.GetValueOrDefault(e.OrganizationalUnitId, string.Empty),
                e.AssetId,
                e.AssetName,
                e.ContractId,
                e.ContractNumber,
                e.SupplierId,
                e.SupplierName,
                e.Type,
                e.Category,
                e.Amount,
                e.Currency,
                e.CompetenceMonth,
                e.EffectiveDate,
                e.Description,
                e.CreatedAt))
            .ToList();

        return new PagedResult<CostEntryDto>(items, query.Page, query.PageSize, total);
    }

    private static IQueryable<CostEntry> WithinEntryScope(
        IQueryable<CostEntry> source,
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
        return source.Where(e => unitIds.Contains(e.OrganizationalUnitId));
    }

    private static IQueryable<Asset> WithinAssetScope(
        IQueryable<Asset> source,
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
        return source.Where(a => unitIds.Contains(a.OrganizationalUnitId));
    }
}
