using ControleAtivos.Application.Abstractions;
using ControleAtivos.Application.Common;

namespace ControleAtivos.Application.Features.Timeline;

public sealed class TimelineService(
    ITimelineQueries queries,
    IOrganizationalScopeResolver scopeResolver)
{
    public async Task<PagedResult<TimelineEventDto>> ListAsync(
        ListTimelineQuery query,
        CancellationToken cancellationToken = default)
    {
        var scope = await scopeResolver.GetAsync(cancellationToken);

        return scope.IsEmpty
            ? PagedResult<TimelineEventDto>.Empty(query.Page, query.PageSize)
            : await queries.ListAsync(query, scope, cancellationToken);
    }
}

public sealed class DashboardService(
    IDashboardQueries queries,
    IOrganizationalScopeResolver scopeResolver)
{
    public async Task<Dashboard.DashboardSummaryDto> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var scope = await scopeResolver.GetAsync(cancellationToken);

        return await queries.GetSummaryAsync(scope, cancellationToken);
    }
}
