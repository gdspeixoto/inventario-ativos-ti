using ControleAtivos.Application.Abstractions;
using ControleAtivos.Application.Features.Reports;
using ControleAtivos.Domain.Entities;
using ControleAtivos.Domain.Enums;
using ControleAtivos.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ControleAtivos.Persistence.Queries;

public sealed class ReportQueries(ControleAtivosDbContext context) : IReportQueries
{
    private static readonly string[] MonthNames =
    [
        "jan", "fev", "mar", "abr", "mai", "jun",
        "jul", "ago", "set", "out", "nov", "dez",
    ];

    /// <summary>
    /// Serie historica de custos.
    ///
    /// A base sao os lancamentos por competencia (<c>cost_entries</c>). Quando
    /// nao existem lancamentos para um mes — situacao comum enquanto a operacao
    /// nao esta madura — o custo corrente dos ativos e usado como referencia,
    /// para que o grafico nao apareca vazio.
    /// </summary>
    public async Task<CostEvolutionReportDto> GetCostEvolutionAsync(
        ReportPeriodQuery query,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default)
    {
        var months = Math.Clamp(query.Months, 3, 36);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = new DateOnly(today.Year, today.Month, 1).AddMonths(-(months - 1));

        var entries = context.CostEntries.AsNoTracking().Where(c => c.CompetenceMonth >= start);

        if (!scope.HasGlobalScope)
        {
            if (scope.IsEmpty)
            {
                return new CostEvolutionReportDto([], [], 0m, 0m, "BRL");
            }

            var unitIds = scope.UnitIds.ToList();
            entries = entries.Where(c => unitIds.Contains(c.OrganizationalUnitId));
        }

        if (query.OrganizationalUnitId is { } filterUnit)
        {
            entries = entries.Where(c => c.OrganizationalUnitId == filterUnit);
        }

        if (query.SupplierId is { } supplierId)
        {
            entries = entries.Where(c => c.SupplierId == supplierId);
        }

        var grouped = await entries
            .GroupBy(c => new { c.CompetenceMonth.Year, c.CompetenceMonth.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Amount = g.Sum(c => c.Amount.Amount),
            })
            .ToListAsync(cancellationToken);

        var assets = WithinAssetScope(context.Assets.AsNoTracking(), scope)
            .Where(a => a.Status == AssetStatus.Ativo);

        if (query.OrganizationalUnitId is { } assetUnit)
        {
            assets = assets.Where(a => a.OrganizationalUnitId == assetUnit);
        }

        if (query.SupplierId is { } assetSupplier)
        {
            assets = assets.Where(a => a.SupplierId == assetSupplier);
        }

        var currentMonthly = await assets.SumAsync(a => (decimal?)a.MonthlyAmount.Amount, cancellationToken)
            ?? 0m;

        var history = new List<CostEvolutionPointDto>();

        for (var i = 0; i < months; i++)
        {
            var reference = start.AddMonths(i);
            var match = grouped.FirstOrDefault(g => g.Year == reference.Year && g.Month == reference.Month);

            history.Add(new CostEvolutionPointDto(
                reference.Year,
                reference.Month,
                $"{MonthNames[reference.Month - 1]}/{reference.Year % 100:D2}",
                match?.Amount ?? currentMonthly,
                "BRL"));
        }

        // Projecao simples: mantem o custo corrente pelos proximos 12 meses.
        var projection = Enumerable.Range(1, 12)
            .Select(offset =>
            {
                var reference = new DateOnly(today.Year, today.Month, 1).AddMonths(offset);

                return new CostEvolutionPointDto(
                    reference.Year,
                    reference.Month,
                    $"{MonthNames[reference.Month - 1]}/{reference.Year % 100:D2}",
                    currentMonthly,
                    "BRL");
            })
            .ToList();

        return new CostEvolutionReportDto(history, projection, currentMonthly, currentMonthly * 12m, "BRL");
    }

    public async Task<IReadOnlyList<LicenseUtilizationReportDto>> GetLicenseUtilizationAsync(
        decimal utilizationBelow,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default) =>
        await WithinAssetScope(context.Licenses.AsNoTracking(), scope)
            .Where(l => l.Status == AssetStatus.Ativo && l.ContractedQuantity > 0)
            .Where(l => (decimal)l.UsedQuantity / l.ContractedQuantity * 100m <= utilizationBelow)
            .OrderBy(l => (decimal)l.UsedQuantity / l.ContractedQuantity)
            .Select(l => new LicenseUtilizationReportDto(
                l.Id,
                l.Name,
                l.Product,
                l.ContractedQuantity,
                l.UsedQuantity,
                l.ContractedQuantity - l.UsedQuantity,
                Math.Round((decimal)l.UsedQuantity / l.ContractedQuantity * 100m, 2),
                l.MonthlyAmount.Amount,
                // Economia potencial ao devolver as licencas ociosas.
                Math.Round(
                    l.MonthlyAmount.Amount / l.ContractedQuantity
                        * (l.ContractedQuantity - l.UsedQuantity),
                    2),
                l.MonthlyAmount.Currency,
                l.OrganizationalUnit.Name))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ContractExpirationReportDto>> GetContractExpirationsAsync(
        int withinDays,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var limit = today.AddDays(withinDays);

        var contracts = context.Contracts.AsNoTracking()
            .Where(c => c.Status == ContractStatus.Ativo)
            .Where(c => c.EndDate >= today && c.EndDate <= limit);

        if (!scope.HasGlobalScope)
        {
            if (scope.IsEmpty)
            {
                return [];
            }

            var unitIds = scope.UnitIds.ToList();
            contracts = contracts.Where(c => unitIds.Contains(c.OrganizationalUnitId));
        }

        return await contracts
            .OrderBy(c => c.EndDate)
            .Select(c => new ContractExpirationReportDto(
                c.Id,
                c.Number,
                c.Name,
                c.Supplier.Name,
                c.MonthlyAmount.Amount,
                c.MonthlyAmount.Currency,
                c.EndDate,
                c.EndDate.DayNumber - today.DayNumber,
                c.OrganizationalUnit.Name))
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<T> WithinAssetScope<T>(IQueryable<T> source, IOrganizationalScope scope)
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
}
