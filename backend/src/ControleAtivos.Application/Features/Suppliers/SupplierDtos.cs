using ControleAtivos.Application.Common;
using ControleAtivos.Domain.Enums;

namespace ControleAtivos.Application.Features.Suppliers;

public sealed record SupplierListItemDto(
    Guid Id,
    string Name,
    string? DocumentNumber,
    ContractCategory? Category,
    SupplierStatus Status,
    string? MainContactName,
    string? Email,
    string? Phone,
    int ActiveContracts,
    int LinkedAssets,
    decimal MonthlyAmount,
    decimal AnnualAmount,
    string Currency);

public sealed record SupplierDetailDto(
    Guid Id,
    string Name,
    string? DocumentNumber,
    ContractCategory? Category,
    SupplierStatus Status,
    string? MainContactName,
    string? Email,
    string? Phone,
    string? Website,
    string? SlaDescription,
    int ActiveContracts,
    int LinkedAssets,
    decimal MonthlyAmount,
    decimal AnnualAmount,
    string Currency,
    IReadOnlyList<SupplierContractDto> Contracts,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record SupplierContractDto(
    Guid Id,
    string Number,
    string Name,
    ContractStatus Status,
    decimal MonthlyAmount,
    DateOnly EndDate);

public sealed record ListSuppliersQuery : PageRequest
{
    public string? Search { get; init; }

    public SupplierStatus? Status { get; init; }

    public ContractCategory? Category { get; init; }
}

public sealed record CreateSupplierCommand
{
    public required string Name { get; init; }

    public string? DocumentNumber { get; init; }

    public ContractCategory? Category { get; init; }

    public string? MainContactName { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public string? Website { get; init; }

    public string? SlaDescription { get; init; }
}

public sealed record UpdateSupplierCommand
{
    public string? MainContactName { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public string? Website { get; init; }

    public ContractCategory? Category { get; init; }

    public string? SlaDescription { get; init; }

    public SupplierStatus? Status { get; init; }
}
