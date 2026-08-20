using ControleAtivos.Application.Abstractions;
using ControleAtivos.Application.Common;
using ControleAtivos.Application.Features.Alerts;
using ControleAtivos.Domain.Entities;
using ControleAtivos.Domain.Enums;
using ControleAtivos.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ControleAtivos.Persistence.Queries;

public sealed class AlertQueries(ControleAtivosDbContext context) : IAlertQueries
{
    public async Task<PagedResult<AlertDto>> ListAsync(
        ListAlertsQuery query,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default)
    {
        var alerts = WithinScope(context.Alerts.AsNoTracking(), scope);

        if (query.Status is { } status)
        {
            alerts = alerts.Where(a => a.Status == status);
        }

        if (query.OnlyOpen == true)
        {
            alerts = alerts.Where(a =>
                a.Status == AlertStatus.Aberto || a.Status == AlertStatus.EmAnalise);
        }

        if (query.Priority is { } priority)
        {
            alerts = alerts.Where(a => a.Priority == priority);
        }

        if (query.Type is { } type)
        {
            alerts = alerts.Where(a => a.Type == type);
        }

        if (query.OrganizationalUnitId is { } unitId)
        {
            alerts = alerts.Where(a => a.OrganizationalUnitId == unitId);
        }

        var total = await alerts.LongCountAsync(cancellationToken);

        if (total == 0)
        {
            return PagedResult<AlertDto>.Empty(query.Page, query.PageSize);
        }

        var items = await alerts
            // Mais graves primeiro, depois os de prazo mais curto.
            .OrderByDescending(a => a.Priority)
            .ThenBy(a => a.DueDate)
            .ThenByDescending(a => a.CreatedAt)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(a => new AlertDto(
                a.Id,
                a.Type,
                a.Priority,
                a.Status,
                a.EntityType,
                a.EntityId,
                a.Title,
                a.Description,
                a.RecommendedAction,
                a.DueDate,
                a.AssignedToUserId,
                a.AssignedToUserId == null
                    ? null
                    : context.Users.Where(u => u.Id == a.AssignedToUserId)
                        .Select(u => u.DisplayName).FirstOrDefault(),
                context.OrganizationalUnits.Where(u => u.Id == a.OrganizationalUnitId)
                    .Select(u => u.Name).FirstOrDefault() ?? "-",
                a.CreatedAt,
                a.ResolvedAt,
                a.ResolutionNotes))
            .ToListAsync(cancellationToken);

        return new PagedResult<AlertDto>(items, query.Page, query.PageSize, total);
    }

    private static IQueryable<Alert> WithinScope(IQueryable<Alert> source, IOrganizationalScope scope)
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

public sealed class AlertRepository(ControleAtivosDbContext context) : IAlertRepository
{
    public Task<Alert?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Alerts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<string>> GetOpenDeduplicationKeysAsync(
        CancellationToken cancellationToken = default) =>
        await context.Alerts
            .AsNoTracking()
            .Where(a => a.Status == AlertStatus.Aberto || a.Status == AlertStatus.EmAnalise)
            .Select(a => a.DeduplicationKey)
            .ToListAsync(cancellationToken);

    public void Add(Alert alert) => context.Alerts.Add(alert);
}
