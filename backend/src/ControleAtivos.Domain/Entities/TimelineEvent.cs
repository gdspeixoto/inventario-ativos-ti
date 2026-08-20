using ControleAtivos.Domain.Common;
using ControleAtivos.Domain.Enums;

namespace ControleAtivos.Domain.Entities;

/// <summary>
/// Linha do historico de uma entidade. Append-only: nao possui metodos de
/// alteracao, e a persistencia bloqueia update e delete. Um historico que pode
/// ser editado nao serve como fonte de auditoria.
/// </summary>
public sealed class TimelineEvent : TenantEntity
{
    private TimelineEvent()
    {
    }

    public TimelineEvent(
        Guid id,
        Guid tenantId,
        TimelineEntityType entityType,
        Guid entityId,
        TimelineEventType eventType,
        string title,
        string description,
        DateTimeOffset occurredAt,
        Guid? organizationalUnitId = null,
        Guid? userId = null,
        string? userDisplayName = null,
        decimal? previousAmount = null,
        decimal? newAmount = null,
        Guid? correlationId = null)
        : base(id, tenantId)
    {
        EntityType = entityType;
        EntityId = Guard.NotEmpty(entityId, nameof(entityId));
        EventType = eventType;
        Title = Guard.MaxLength(Guard.NotEmpty(title, nameof(title)), 200, nameof(title));
        Description = Guard.MaxLength(Guard.NotEmpty(description, nameof(description)), 2000, nameof(description));
        OccurredAt = occurredAt;
        OrganizationalUnitId = organizationalUnitId;
        UserId = userId;
        UserDisplayName = Guard.Optional(userDisplayName);
        PreviousAmount = previousAmount;
        NewAmount = newAmount;
        CorrelationId = correlationId;
    }

    public TimelineEntityType EntityType { get; private set; }

    public Guid EntityId { get; private set; }

    public TimelineEventType EventType { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; private set; }

    public Guid? OrganizationalUnitId { get; private set; }

    public Guid? UserId { get; private set; }

    /// <summary>
    /// Nome do autor no momento do evento. Desnormalizado de proposito: se a
    /// pessoa sair da instituicao, o historico continua legivel.
    /// </summary>
    public string? UserDisplayName { get; private set; }

    public decimal? PreviousAmount { get; private set; }

    public decimal? NewAmount { get; private set; }

    /// <summary>Diferenca financeira do evento, quando houver.</summary>
    public decimal? FinancialImpact =>
        PreviousAmount is null || NewAmount is null ? null : NewAmount - PreviousAmount;

    public bool HasFinancialImpact => FinancialImpact is not null && FinancialImpact != 0m;

    /// <summary>Correlaciona eventos gerados pela mesma requisicao HTTP.</summary>
    public Guid? CorrelationId { get; private set; }
}
