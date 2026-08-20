using ControleAtivos.Domain.Common;

namespace ControleAtivos.Domain.Entities;

/// <summary>
/// Registro tecnico de acesso e mutacao. Complementa a timeline: enquanto a
/// timeline conta a historia do ativo em linguagem de negocio, o audit log
/// responde "quem chamou qual endpoint, de qual IP, quando" — o que interessa
/// numa investigacao de seguranca.
/// </summary>
public sealed class AuditLog : TenantEntity
{
    private AuditLog()
    {
    }

    public AuditLog(
        Guid id,
        Guid tenantId,
        string action,
        string entityType,
        Guid? entityId,
        Guid? userId,
        string? userDisplayName,
        string? ipAddress,
        string? userAgent,
        Guid? correlationId,
        string? metadataJson = null)
        : base(id, tenantId)
    {
        Action = Guard.MaxLength(Guard.NotEmpty(action, nameof(action)), 120, nameof(action));
        EntityType = Guard.MaxLength(Guard.NotEmpty(entityType, nameof(entityType)), 120, nameof(entityType));
        EntityId = entityId;
        UserId = userId;
        UserDisplayName = Guard.Optional(userDisplayName);
        IpAddress = Guard.Optional(ipAddress);
        UserAgent = Guard.Optional(userAgent)?[..Math.Min(Guard.Optional(userAgent)!.Length, 500)];
        CorrelationId = correlationId;
        MetadataJson = Guard.Optional(metadataJson);
        OccurredAt = DateTimeOffset.UtcNow;
    }

    public string Action { get; private set; } = string.Empty;

    public string EntityType { get; private set; } = string.Empty;

    public Guid? EntityId { get; private set; }

    public Guid? UserId { get; private set; }

    public string? UserDisplayName { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public Guid? CorrelationId { get; private set; }

    /// <summary>
    /// Dados adicionais em JSON. Nao deve conter token, cookie, senha ou
    /// qualquer segredo — a camada de API e responsavel por filtrar.
    /// </summary>
    public string? MetadataJson { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }
}
