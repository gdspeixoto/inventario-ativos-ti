using ControleAtivos.Application.Abstractions;
using ControleAtivos.Application.Common;
using ControleAtivos.Application.Features.Timeline;
using ControleAtivos.Domain.Entities;
using ControleAtivos.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ControleAtivos.Persistence.Queries;

public sealed class TimelineQueries(ControleAtivosDbContext context) : ITimelineQueries
{
    public async Task<PagedResult<TimelineEventDto>> ListAsync(
        ListTimelineQuery query,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default)
    {
        var events = WithinScope(context.TimelineEvents.AsNoTracking(), scope);

        if (query.EntityType is { } entityType)
        {
            events = events.Where(e => e.EntityType == entityType);
        }

        if (query.EntityId is { } entityId)
        {
            events = events.Where(e => e.EntityId == entityId);
        }

        if (query.EventType is { } eventType)
        {
            events = events.Where(e => e.EventType == eventType);
        }

        if (query.UserId is { } userId)
        {
            events = events.Where(e => e.UserId == userId);
        }

        if (query.OrganizationalUnitId is { } unitId)
        {
            events = events.Where(e => e.OrganizationalUnitId == unitId);
        }

        if (query.From is { } from)
        {
            events = events.Where(e => e.OccurredAt >= from);
        }

        if (query.To is { } to)
        {
            events = events.Where(e => e.OccurredAt <= to);
        }

        if (query.OnlyFinancial == true)
        {
            events = events.Where(e =>
                e.PreviousAmount != null && e.NewAmount != null && e.PreviousAmount != e.NewAmount);
        }

        var total = await events.LongCountAsync(cancellationToken);

        if (total == 0)
        {
            return PagedResult<TimelineEventDto>.Empty(query.Page, query.PageSize);
        }

        var items = await events
            // Historico se le do mais recente para o mais antigo.
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.Id)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(e => new TimelineEventDto(
                e.Id,
                e.EntityType,
                e.EntityId,
                e.EventType,
                e.Title,
                e.Description,
                e.OccurredAt,
                e.UserId,
                e.UserDisplayName,
                e.PreviousAmount,
                e.NewAmount,
                e.PreviousAmount != null && e.NewAmount != null
                    ? e.NewAmount - e.PreviousAmount
                    : null,
                e.OrganizationalUnitId,
                context.OrganizationalUnits
                    .Where(u => u.Id == e.OrganizationalUnitId)
                    .Select(u => u.Name)
                    .FirstOrDefault(),
                e.CorrelationId))
            .ToListAsync(cancellationToken);

        return new PagedResult<TimelineEventDto>(items, query.Page, query.PageSize, total);
    }

    /// <summary>
    /// Eventos sem unidade organizacional (acoes administrativas) so aparecem
    /// para quem tem escopo global.
    /// </summary>
    private static IQueryable<TimelineEvent> WithinScope(
        IQueryable<TimelineEvent> source,
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
        return source.Where(e =>
            e.OrganizationalUnitId != null && unitIds.Contains(e.OrganizationalUnitId.Value));
    }
}
