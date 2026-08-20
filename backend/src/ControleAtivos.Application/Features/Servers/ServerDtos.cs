using ControleAtivos.Application.Common;
using ControleAtivos.Domain.Enums;

namespace ControleAtivos.Application.Features.Servers;

public sealed record ServerListItemDto(
    Guid Id,
    string Name,
    string Code,
    string Hostname,
    ServerType ServerType,
    ServerEnvironment Environment,
    AssetStatus Status,
    string? OperatingSystem,
    string? Provider,
    int? CpuCores,
    int? MemoryGb,
    int? StorageGb,
    decimal MonthlyAmount,
    decimal AnnualAmount,
    string Currency,
    string? TechnicalResponsibleName,
    string OrganizationalUnitName,
    DateTimeOffset? LastBackupAt,
    bool HasResponsible);

public sealed record ServerDetailDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    string Hostname,
    ServerType ServerType,
    ServerEnvironment Environment,
    AssetStatus Status,
    string? OperatingSystem,
    string? OperatingSystemVersion,
    string? Provider,
    string? RegionOrDatacenter,
    string? PrimaryIp,
    int? CpuCores,
    int? MemoryGb,
    int? StorageGb,
    string? BackupPolicy,
    DateTimeOffset? LastBackupAt,
    string? Sla,
    string? MaintenanceWindow,
    decimal InfrastructureMonthlyCost,
    decimal LicenseMonthlyCost,
    decimal SupportMonthlyCost,
    decimal BackupMonthlyCost,
    decimal MonthlyAmount,
    decimal AnnualAmount,
    string Currency,
    Guid OrganizationalUnitId,
    string OrganizationalUnitName,
    string OrganizationalUnitPath,
    Guid? SupplierId,
    string? SupplierName,
    Guid? ContractId,
    string? ContractNumber,
    Guid? CostCenterId,
    string? CostCenterName,
    Guid? TechnicalResponsibleUserId,
    string? TechnicalResponsibleName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record ListServersQuery : PageRequest
{
    public string? Search { get; init; }

    public AssetStatus? Status { get; init; }

    public ServerEnvironment? Environment { get; init; }

    public ServerType? ServerType { get; init; }

    public string? Provider { get; init; }

    public Guid? OrganizationalUnitId { get; init; }

    /// <summary>Somente servidores sem responsavel tecnico definido.</summary>
    public bool? WithoutResponsible { get; init; }

    /// <summary>Sem backup nos ultimos N dias.</summary>
    public int? WithoutBackupForDays { get; init; }

    /// <summary>Custo mensal acima deste valor.</summary>
    public decimal? MonthlyCostAbove { get; init; }

    public ServerSortField SortBy { get; init; } = ServerSortField.Name;

    public bool Descending { get; init; }
}

public enum ServerSortField
{
    Name = 0,
    MonthlyAmount = 1,
    Environment = 2,
    CreatedAt = 3,
}

public sealed record CreateServerCommand
{
    public required string Name { get; init; }

    public required string Code { get; init; }

    public required string Hostname { get; init; }

    public required Guid OrganizationalUnitId { get; init; }

    public required ServerType ServerType { get; init; }

    public required ServerEnvironment Environment { get; init; }

    public required decimal InfrastructureMonthlyCost { get; init; }

    public string Currency { get; init; } = "BRL";

    public string? Description { get; init; }

    public string? OperatingSystem { get; init; }

    public string? OperatingSystemVersion { get; init; }

    public string? Provider { get; init; }

    public string? RegionOrDatacenter { get; init; }

    public string? PrimaryIp { get; init; }

    public int? CpuCores { get; init; }

    public int? MemoryGb { get; init; }

    public int? StorageGb { get; init; }

    public Guid? SupplierId { get; init; }

    public Guid? ContractId { get; init; }

    public Guid? CostCenterId { get; init; }

    public Guid? TechnicalResponsibleUserId { get; init; }
}

public sealed record UpdateServerCostsCommand
{
    public required decimal InfrastructureMonthlyCost { get; init; }

    public required decimal LicenseMonthlyCost { get; init; }

    public required decimal SupportMonthlyCost { get; init; }

    public required decimal BackupMonthlyCost { get; init; }

    public required string Reason { get; init; }

    public string Currency { get; init; } = "BRL";
}

public sealed record ResizeServerCommand
{
    public int? CpuCores { get; init; }

    public int? MemoryGb { get; init; }

    public int? StorageGb { get; init; }

    public required string Reason { get; init; }
}

public sealed record DecommissionServerCommand
{
    public required string Reason { get; init; }
}

public sealed record ServerCostChangeResultDto(
    Guid ServerId,
    decimal PreviousMonthlyAmount,
    decimal NewMonthlyAmount,
    decimal MonthlyImpact,
    decimal AnnualImpact,
    string Currency);

/// <summary>Correcao dos dados cadastrais de um servidor.</summary>
public sealed record UpdateServerCommand
{
    public required string Name { get; init; }

    public required string Code { get; init; }

    public required string Hostname { get; init; }

    public required ServerType ServerType { get; init; }

    public required ServerEnvironment Environment { get; init; }

    public string? Description { get; init; }

    public string? OperatingSystem { get; init; }

    public string? OperatingSystemVersion { get; init; }

    public string? Provider { get; init; }

    public string? RegionOrDatacenter { get; init; }

    public string? PrimaryIp { get; init; }
}
