using ControleAtivos.Application.Common;
using ControleAtivos.Domain.Enums;

namespace ControleAtivos.Application.Features.Contracts;

public sealed record ContractListItemDto(
    Guid Id,
    string Number,
    string Name,
    ContractCategory Category,
    ContractStatus Status,
    Guid SupplierId,
    string SupplierName,
    string OrganizationalUnitName,
    decimal MonthlyAmount,
    decimal AnnualAmount,
    string Currency,
    DateOnly StartDate,
    DateOnly EndDate,
    int DaysUntilExpiration,
    AdjustmentIndex AdjustmentIndex,
    DateOnly? NextAdjustmentDate,
    int LinkedAssetCount,
    string? InternalResponsibleName);

public sealed record ContractDetailDto(
    Guid Id,
    string Number,
    string Name,
    string? Description,
    ContractCategory Category,
    ContractStatus Status,
    Guid SupplierId,
    string SupplierName,
    Guid OrganizationalUnitId,
    string OrganizationalUnitName,
    string OrganizationalUnitPath,
    decimal MonthlyAmount,
    decimal AnnualAmount,
    string Currency,
    DateOnly StartDate,
    DateOnly EndDate,
    int DaysUntilExpiration,
    AdjustmentIndex AdjustmentIndex,
    AdjustmentPeriodicity AdjustmentPeriodicity,
    DateOnly? LastAdjustmentDate,
    DateOnly? NextAdjustmentDate,
    Guid? InternalResponsibleUserId,
    string? InternalResponsibleName,
    IReadOnlyList<LinkedAssetDto> LinkedAssets,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record LinkedAssetDto(
    Guid Id,
    string Name,
    string Code,
    AssetKind Kind,
    AssetStatus Status,
    decimal MonthlyAmount);

public sealed record ListContractsQuery : PageRequest
{
    public string? Search { get; init; }

    public ContractStatus? Status { get; init; }

    public ContractCategory? Category { get; init; }

    public Guid? SupplierId { get; init; }

    public Guid? OrganizationalUnitId { get; init; }

    /// <summary>Vencimento dentro dos proximos N dias.</summary>
    public int? ExpiringWithinDays { get; init; }

    public ContractSortField SortBy { get; init; } = ContractSortField.EndDate;

    public bool Descending { get; init; }
}

public enum ContractSortField
{
    Number = 0,
    Name = 1,
    MonthlyAmount = 2,
    EndDate = 3,
}

public sealed record CreateContractCommand
{
    public required string Number { get; init; }

    public required string Name { get; init; }

    public required Guid SupplierId { get; init; }

    public required Guid OrganizationalUnitId { get; init; }

    public required ContractCategory Category { get; init; }

    public required DateOnly StartDate { get; init; }

    public required DateOnly EndDate { get; init; }

    public required decimal MonthlyAmount { get; init; }

    public string Currency { get; init; } = "BRL";

    public string? Description { get; init; }

    public AdjustmentIndex AdjustmentIndex { get; init; } = AdjustmentIndex.NegociacaoManual;

    public AdjustmentPeriodicity AdjustmentPeriodicity { get; init; } = AdjustmentPeriodicity.Anual;

    public Guid? InternalResponsibleUserId { get; init; }
}

public sealed record RenewContractCommand
{
    public required DateOnly NewEndDate { get; init; }

    public string? Reason { get; init; }
}

public sealed record TerminateContractCommand
{
    public required ContractStatus Status { get; init; }

    public required string Reason { get; init; }
}
