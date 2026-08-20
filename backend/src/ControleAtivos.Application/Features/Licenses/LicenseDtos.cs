using ControleAtivos.Application.Common;
using ControleAtivos.Domain.Enums;

namespace ControleAtivos.Application.Features.Licenses;

/// <summary>Linha da listagem de licencas.</summary>
public sealed record LicenseListItemDto(
    Guid Id,
    string Name,
    string Code,
    string Manufacturer,
    string Product,
    string? Plan,
    AssetStatus Status,
    BillingType BillingType,
    int ContractedQuantity,
    int UsedQuantity,
    int AvailableQuantity,
    decimal UtilizationRate,
    decimal UnitPrice,
    decimal MonthlyAmount,
    decimal AnnualAmount,
    string Currency,
    string? SupplierName,
    string OrganizationalUnitName,
    DateOnly? RenewalDate,
    DateOnly? LastAdjustmentDate,
    DateOnly? NextAdjustmentDate);

/// <summary>Detalhe completo de uma licenca.</summary>
public sealed record LicenseDetailDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    string Manufacturer,
    string Product,
    string? Plan,
    AssetStatus Status,
    BillingType BillingType,
    int ContractedQuantity,
    int UsedQuantity,
    int AvailableQuantity,
    decimal UtilizationRate,
    decimal UnitPrice,
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
    Guid? FinancialResponsibleUserId,
    string? FinancialResponsibleName,
    DateOnly? StartDate,
    DateOnly? RenewalDate,
    DateOnly? LastAdjustmentDate,
    DateOnly? NextAdjustmentDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

/// <summary>Filtros da listagem de licencas.</summary>
public sealed record ListLicensesQuery : PageRequest
{
    public string? Search { get; init; }

    public AssetStatus? Status { get; init; }

    public string? Manufacturer { get; init; }

    public Guid? SupplierId { get; init; }

    public Guid? OrganizationalUnitId { get; init; }

    public Guid? CostCenterId { get; init; }

    /// <summary>Renovacao dentro dos proximos N dias.</summary>
    public int? RenewalWithinDays { get; init; }

    /// <summary>Utilizacao abaixo deste percentual (licencas ociosas).</summary>
    public decimal? UtilizationBelow { get; init; }

    /// <summary>Utilizacao acima deste percentual (risco de estouro).</summary>
    public decimal? UtilizationAbove { get; init; }

    public LicenseSortField SortBy { get; init; } = LicenseSortField.Name;

    public bool Descending { get; init; }
}

public enum LicenseSortField
{
    Name = 0,
    MonthlyAmount = 1,
    RenewalDate = 2,
    Utilization = 3,
    CreatedAt = 4,
}

/// <summary>Dados para cadastro de uma licenca.</summary>
public sealed record CreateLicenseCommand
{
    public required string Name { get; init; }

    public required string Code { get; init; }

    public required string Manufacturer { get; init; }

    public required string Product { get; init; }

    public string? Plan { get; init; }

    public string? Description { get; init; }

    public required Guid OrganizationalUnitId { get; init; }

    public required int ContractedQuantity { get; init; }

    public required decimal UnitPrice { get; init; }

    public string Currency { get; init; } = "BRL";

    public BillingType BillingType { get; init; } = BillingType.Mensal;

    public Guid? SupplierId { get; init; }

    public Guid? ContractId { get; init; }

    public Guid? CostCenterId { get; init; }

    public Guid? TechnicalResponsibleUserId { get; init; }

    public Guid? FinancialResponsibleUserId { get; init; }

    public DateOnly? StartDate { get; init; }

    public DateOnly? RenewalDate { get; init; }
}

/// <summary>
/// Reajuste de preco. O cliente informa apenas o valor novo — diferenca,
/// percentual e impacto anual sao calculados pelo dominio.
/// </summary>
public sealed record ApplyPriceAdjustmentCommand
{
    public required decimal NewMonthlyAmount { get; init; }

    public required DateOnly EffectiveDate { get; init; }

    public required string Reason { get; init; }

    public AdjustmentIndex IndexApplied { get; init; } = AdjustmentIndex.NegociacaoManual;

    public Guid? ApprovedByUserId { get; init; }
}

public sealed record ChangeLicenseQuantityCommand
{
    public required int NewQuantity { get; init; }

    public required string Reason { get; init; }
}

public sealed record UpdateLicenseUsageCommand
{
    public required int UsedQuantity { get; init; }
}

/// <summary>Resultado de um reajuste, com o impacto ja calculado.</summary>
public sealed record PriceAdjustmentResultDto(
    Guid Id,
    decimal PreviousAmount,
    decimal NewAmount,
    decimal AbsoluteDifference,
    decimal PercentageDifference,
    decimal AnnualImpact,
    string Currency,
    DateOnly EffectiveDate,
    string Reason,
    AdjustmentIndex IndexApplied,
    bool ExceedsAlertThreshold);

public sealed record QuantityChangeResultDto(
    Guid LicenseId,
    int PreviousQuantity,
    int NewQuantity,
    decimal PreviousMonthlyAmount,
    decimal NewMonthlyAmount,
    decimal MonthlyImpact,
    decimal AnnualImpact,
    string Currency);

/// <summary>Simulacao de reajuste: nada e persistido.</summary>
public sealed record SimulateAdjustmentQuery
{
    public required decimal CurrentAmount { get; init; }

    public required decimal NewAmount { get; init; }

    public string Currency { get; init; } = "BRL";
}

public sealed record SimulationResultDto(
    decimal CurrentAmount,
    decimal NewAmount,
    decimal AbsoluteDifference,
    decimal PercentageDifference,
    decimal MonthlyImpact,
    decimal AnnualImpact,
    string Currency);

/// <summary>
/// Correcao dos dados cadastrais de uma licenca.
///
/// Nao altera valor, quantidade nem situacao: para isso existem o reajuste, a
/// mudanca de quantidade e a desativacao. Aqui se conserta o que foi digitado
/// errado.
/// </summary>
public sealed record UpdateLicenseCommand
{
    public required string Name { get; init; }

    public required string Code { get; init; }

    public required string Manufacturer { get; init; }

    public required string Product { get; init; }

    public string? Plan { get; init; }

    public string? Description { get; init; }

    public BillingType BillingType { get; init; } = BillingType.Mensal;

    public DateOnly? StartDate { get; init; }

    public DateOnly? RenewalDate { get; init; }
}

/// <summary>
/// Cancelamento de uma licenca.
///
/// E a contraparte da exclusao: quando a licenca teve vida real — foi paga,
/// reajustada — encerra-la preserva o historico, e a justificativa explica a
/// quem consultar depois por que ela saiu de uso.
/// </summary>
public sealed record CancelLicenseCommand
{
    public required string Reason { get; init; }
}
