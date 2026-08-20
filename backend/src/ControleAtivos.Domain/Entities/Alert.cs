using ControleAtivos.Domain.Common;
using ControleAtivos.Domain.Enums;

namespace ControleAtivos.Domain.Entities;

/// <summary>
/// Pendencia detectada pelo sistema (vencimento proximo, reajuste fora do
/// limite, servidor sem backup, licenca ociosa).
///
/// A <see cref="DeduplicationKey"/> evita que a rotina de deteccao crie um
/// alerta novo a cada execucao para o mesmo problema ainda em aberto.
/// </summary>
public sealed class Alert : TenantEntity
{
    private Alert()
    {
    }

    public Alert(
        Guid id,
        Guid tenantId,
        AlertType type,
        AlertPriority priority,
        TimelineEntityType entityType,
        Guid entityId,
        Guid organizationalUnitId,
        string title,
        string description,
        string recommendedAction,
        DateOnly? dueDate = null)
        : base(id, tenantId)
    {
        Type = type;
        Priority = priority;
        EntityType = entityType;
        EntityId = Guard.NotEmpty(entityId, nameof(entityId));
        OrganizationalUnitId = organizationalUnitId;
        Title = Guard.MaxLength(Guard.NotEmpty(title, nameof(title)), 200, nameof(title));
        Description = Guard.MaxLength(Guard.NotEmpty(description, nameof(description)), 2000, nameof(description));
        RecommendedAction = Guard.MaxLength(
            Guard.NotEmpty(recommendedAction, nameof(recommendedAction)),
            1000,
            nameof(recommendedAction));
        DueDate = dueDate;
        Status = AlertStatus.Aberto;
        DeduplicationKey = BuildDeduplicationKey(type, entityId);
    }

    public AlertType Type { get; private set; }

    public AlertPriority Priority { get; private set; }

    public AlertStatus Status { get; private set; }

    public TimelineEntityType EntityType { get; private set; }

    public Guid EntityId { get; private set; }

    public Guid OrganizationalUnitId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string RecommendedAction { get; private set; } = string.Empty;

    public DateOnly? DueDate { get; private set; }

    public Guid? AssignedToUserId { get; private set; }

    public Guid? ResolvedByUserId { get; private set; }

    public DateTimeOffset? ResolvedAt { get; private set; }

    public string? ResolutionNotes { get; private set; }

    /// <summary>Chave estavel <c>tipo:entidade</c> usada para evitar duplicidade.</summary>
    public string DeduplicationKey { get; private set; } = string.Empty;

    public bool IsOpen => Status is AlertStatus.Aberto or AlertStatus.EmAnalise;

    public static string BuildDeduplicationKey(AlertType type, Guid entityId) => $"{type}:{entityId}";

    public void AssignTo(AppUser user)
    {
        DomainException.ThrowIf(user.TenantId != TenantId, "O usuario pertence a outro tenant.");
        AssignedToUserId = user.Id;
        Status = AlertStatus.EmAnalise;
        Touch(user.Id);
    }

    public void Escalate(AlertPriority priority)
    {
        DomainException.ThrowIf(priority <= Priority, "A nova prioridade deve ser maior que a atual.");
        Priority = priority;
        Touch();
    }

    public void Resolve(Guid userId, string notes)
    {
        DomainException.ThrowIf(!IsOpen, "Este alerta ja foi encerrado.");
        Status = AlertStatus.Resolvido;
        ResolvedByUserId = userId;
        ResolvedAt = DateTimeOffset.UtcNow;
        ResolutionNotes = Guard.MaxLength(Guard.NotEmpty(notes, nameof(notes)), 1000, nameof(notes));
        Touch(userId);
    }

    public void Ignore(Guid userId, string justification)
    {
        DomainException.ThrowIf(!IsOpen, "Este alerta ja foi encerrado.");
        Status = AlertStatus.Ignorado;
        ResolvedByUserId = userId;
        ResolvedAt = DateTimeOffset.UtcNow;
        ResolutionNotes = Guard.MaxLength(
            Guard.NotEmpty(justification, nameof(justification)),
            1000,
            nameof(justification));
        Touch(userId);
    }
}
