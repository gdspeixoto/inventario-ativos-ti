namespace ControleAtivos.Application.Features.Reports;

/// <summary>Serie mensal de custos, para o grafico de evolucao.</summary>
public sealed record CostEvolutionPointDto(
    int Year,
    int Month,
    string Label,
    decimal Amount,
    string Currency);

public sealed record CostEvolutionReportDto(
    IReadOnlyList<CostEvolutionPointDto> History,
    IReadOnlyList<CostEvolutionPointDto> Projection,
    decimal CurrentMonthlyCost,
    decimal ProjectedAnnualCost,
    string Currency);

public sealed record LicenseUtilizationReportDto(
    Guid Id,
    string Name,
    string Product,
    int ContractedQuantity,
    int UsedQuantity,
    int AvailableQuantity,
    decimal UtilizationRate,
    decimal MonthlyAmount,
    decimal PotentialMonthlySavings,
    string Currency,
    string OrganizationalUnitName);

public sealed record ContractExpirationReportDto(
    Guid Id,
    string Number,
    string Name,
    string SupplierName,
    decimal MonthlyAmount,
    string Currency,
    DateOnly EndDate,
    int DaysUntilExpiration,
    string OrganizationalUnitName);

/// <summary>Parametros comuns aos relatorios com recorte temporal.</summary>
public sealed record ReportPeriodQuery
{
    public int Months { get; init; } = 12;

    public Guid? OrganizationalUnitId { get; init; }

    public Guid? SupplierId { get; init; }
}
