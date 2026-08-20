using ControleAtivos.Application.Common;
using ControleAtivos.Domain.Enums;

namespace ControleAtivos.Application.Features.Alerts;

public sealed record AlertDto(
    Guid Id,
    AlertType Type,
    AlertPriority Priority,
    AlertStatus Status,
    TimelineEntityType EntityType,
    Guid EntityId,
    string Title,
    string Description,
    string RecommendedAction,
    DateOnly? DueDate,
    Guid? AssignedToUserId,
    string? AssignedToName,
    string OrganizationalUnitName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt,
    string? ResolutionNotes);

public sealed record ListAlertsQuery : PageRequest
{
    public AlertStatus? Status { get; init; }

    public AlertPriority? Priority { get; init; }

    public AlertType? Type { get; init; }

    public Guid? OrganizationalUnitId { get; init; }

    /// <summary>Somente alertas em aberto (novos ou em analise).</summary>
    public bool? OnlyOpen { get; init; }
}

public sealed record ResolveAlertCommand
{
    public required string Notes { get; init; }
}

public sealed record IgnoreAlertCommand
{
    public required string Justification { get; init; }
}

/// <summary>Resultado da varredura que detecta pendencias.</summary>
public sealed record AlertScanResultDto(
    int Created,
    int AlreadyOpen,
    int AutoResolved,
    IReadOnlyList<AlertDto> NewAlerts);

/// <summary>Limites que definem quando uma situacao vira alerta.</summary>
public sealed record AlertScanSettings
{
    public int ContractExpirationDays { get; init; } = 60;

    public int LicenseRenewalDays { get; init; } = 45;

    public int BackupMaxAgeDays { get; init; } = 3;

    public decimal IdleUtilizationBelow { get; init; } = 50m;

    public decimal HighUtilizationAbove { get; init; } = 90m;

    public decimal AdjustmentThresholdPercentage { get; init; } = 10m;
}
