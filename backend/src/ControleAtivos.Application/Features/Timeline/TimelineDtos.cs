using ControleAtivos.Application.Common;
using ControleAtivos.Domain.Enums;

namespace ControleAtivos.Application.Features.Timeline;

public sealed record TimelineEventDto(
    Guid Id,
    TimelineEntityType EntityType,
    Guid EntityId,
    TimelineEventType EventType,
    string Title,
    string Description,
    DateTimeOffset OccurredAt,
    Guid? UserId,
    string? UserDisplayName,
    decimal? PreviousAmount,
    decimal? NewAmount,
    decimal? FinancialImpact,
    Guid? OrganizationalUnitId,
    string? OrganizationalUnitName,
    Guid? CorrelationId);

public sealed record ListTimelineQuery : PageRequest
{
    /// <summary>Restringe a timeline a uma entidade especifica.</summary>
    public TimelineEntityType? EntityType { get; init; }

    public Guid? EntityId { get; init; }

    public TimelineEventType? EventType { get; init; }

    public Guid? UserId { get; init; }

    public Guid? OrganizationalUnitId { get; init; }

    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }

    /// <summary>Somente eventos que alteraram valores.</summary>
    public bool? OnlyFinancial { get; init; }
}
