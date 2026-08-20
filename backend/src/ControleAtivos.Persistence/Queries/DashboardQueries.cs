using ControleAtivos.Application.Abstractions;
using ControleAtivos.Application.Features.Dashboard;
using ControleAtivos.Application.Features.Timeline;
using ControleAtivos.Domain.Entities;
using ControleAtivos.Domain.Enums;
using ControleAtivos.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ControleAtivos.Persistence.Queries;

/// <summary>Agregacoes da tela inicial.</summary>
public sealed class DashboardQueries(ControleAtivosDbContext context) : IDashboardQueries
{
    private const int IdleUtilizationThreshold = 50;

    public async Task<DashboardSummaryDto> GetSummaryAsync(
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default)
    {
        var assets = WithinScope(context.Assets.AsNoTracking(), scope)
            .Where(a => a.Status == AssetStatus.Ativo);

        var totals = await assets
            .GroupBy(_ => 1)
            .Select(g => new
            {
                MonthlyCost = g.Sum(a => a.MonthlyAmount.Amount),
                Licenses = g.Count(a => a.Kind == AssetKind.License),
                Servers = g.Count(a => a.Kind == AssetKind.Server),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var licenses = await WithinScope(context.Licenses.AsNoTracking(), scope)
            .Where(l => l.Status == AssetStatus.Ativo)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Contracted = g.Sum(l => l.ContractedQuantity),
                Used = g.Sum(l => l.UsedQuantity),
                Idle = g.Count(l => l.ContractedQuantity > 0
                    && (decimal)l.UsedQuantity / l.ContractedQuantity * 100m < IdleUtilizationThreshold),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var contracts = context.Contracts
            .AsNoTracking()
            .Where(c => c.Status == ContractStatus.Ativo);

        if (!scope.HasGlobalScope)
        {
            var unitIds = scope.UnitIds.ToList();
            contracts = contracts.Where(c => unitIds.Contains(c.OrganizationalUnitId));
        }

        var expiring = await contracts
            .Where(c => c.EndDate >= today)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                In30 = g.Count(c => c.EndDate <= today.AddDays(30)),
                In60 = g.Count(c => c.EndDate <= today.AddDays(60)),
                In90 = g.Count(c => c.EndDate <= today.AddDays(90)),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var alerts = context.Alerts.AsNoTracking()
            .Where(a => a.Status == AlertStatus.Aberto || a.Status == AlertStatus.EmAnalise);

        if (!scope.HasGlobalScope)
        {
            var unitIds = scope.UnitIds.ToList();
            alerts = alerts.Where(a => unitIds.Contains(a.OrganizationalUnitId));
        }

        var alertTotals = await alerts
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Open = g.Count(),
                Critical = g.Count(a => a.Priority == AlertPriority.Critica),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var costsByCategory = await assets
            .GroupBy(a => a.Kind)
            .Select(g => new CostByCategoryDto(
                g.Key.ToString(),
                g.Sum(a => a.MonthlyAmount.Amount),
                g.Sum(a => a.MonthlyAmount.Amount) * 12m))
            .ToListAsync(cancellationToken);

        var topCosts = await assets
            .OrderByDescending(a => a.MonthlyAmount.Amount)
            .Take(10)
            .Select(a => new TopCostDto(
                a.Id,
                a.Name,
                a.Kind.ToString(),
                a.MonthlyAmount.Amount,
                a.MonthlyAmount.Amount * 12m))
            .ToListAsync(cancellationToken);

        var serversByEnvironment = await WithinScope(context.Servers.AsNoTracking(), scope)
            .Where(s => s.Status == AssetStatus.Ativo)
            .GroupBy(s => s.Environment)
            .Select(g => new ServersByEnvironmentDto(
                g.Key.ToString(),
                g.Count(),
                g.Sum(s => s.MonthlyAmount.Amount)))
            .ToListAsync(cancellationToken);

        var latestEvents = await WithinTimelineScope(context.TimelineEvents.AsNoTracking(), scope)
            .OrderByDescending(e => e.OccurredAt)
            .Take(10)
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
                null,
                e.CorrelationId))
            .ToListAsync(cancellationToken);

        var monthlyCost = totals?.MonthlyCost ?? 0m;

        return new DashboardSummaryDto(
            monthlyCost,
            monthlyCost * 12m,
            "BRL",
            totals?.Licenses ?? 0,
            totals?.Servers ?? 0,
            licenses?.Contracted ?? 0,
            licenses?.Used ?? 0,
            licenses?.Idle ?? 0,
            expiring?.In30 ?? 0,
            expiring?.In60 ?? 0,
            expiring?.In90 ?? 0,
            alertTotals?.Open ?? 0,
            alertTotals?.Critical ?? 0,
            costsByCategory,
            topCosts,
            serversByEnvironment,
            latestEvents);
    }

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

    private static IQueryable<TimelineEvent> WithinTimelineScope(
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
