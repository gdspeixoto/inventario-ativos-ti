using ControleAtivos.Application.Abstractions;
using ControleAtivos.Application.Common;
using ControleAtivos.Application.Features.Licenses;

namespace ControleAtivos.Application.Features.Costs;

public sealed class CostService(
    ICostQueries queries,
    IOrganizationalScopeResolver scopeResolver)
{
    public async Task<PagedResult<PriceAdjustmentListItemDto>> ListAdjustmentsAsync(
        ListPriceAdjustmentsQuery query,
        CancellationToken cancellationToken = default)
    {
        var scope = await scopeResolver.GetAsync(cancellationToken);

        return scope.IsEmpty
            ? PagedResult<PriceAdjustmentListItemDto>.Empty(query.Page, query.PageSize)
            : await queries.ListAdjustmentsAsync(
                query,
                LicenseService.DefaultAdjustmentAlertThreshold,
                scope,
                cancellationToken);
    }

    public async Task<CostSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var scope = await scopeResolver.GetAsync(cancellationToken);

        return await queries.GetSummaryAsync(
            LicenseService.DefaultAdjustmentAlertThreshold,
            scope,
            cancellationToken);
    }
}

public sealed class ReportService(
    IReportQueries queries,
    IOrganizationalScopeResolver scopeResolver)
{
    public async Task<Reports.CostEvolutionReportDto> GetCostEvolutionAsync(
        Reports.ReportPeriodQuery query,
        CancellationToken cancellationToken = default)
    {
        var scope = await scopeResolver.GetAsync(cancellationToken);

        return await queries.GetCostEvolutionAsync(query, scope, cancellationToken);
    }

    public async Task<IReadOnlyList<Reports.LicenseUtilizationReportDto>> GetLicenseUtilizationAsync(
        decimal utilizationBelow,
        CancellationToken cancellationToken = default)
    {
        var scope = await scopeResolver.GetAsync(cancellationToken);

        return scope.IsEmpty
            ? []
            : await queries.GetLicenseUtilizationAsync(utilizationBelow, scope, cancellationToken);
    }

    public async Task<IReadOnlyList<Reports.ContractExpirationReportDto>> GetContractExpirationsAsync(
        int withinDays,
        CancellationToken cancellationToken = default)
    {
        var scope = await scopeResolver.GetAsync(cancellationToken);

        return scope.IsEmpty
            ? []
            : await queries.GetContractExpirationsAsync(withinDays, scope, cancellationToken);
    }
}
