using ControleAtivos.Application.Features.Timeline;

namespace ControleAtivos.Application.Features.Dashboard;

/// <summary>Visao consolidada exibida na tela inicial.</summary>
public sealed record DashboardSummaryDto(
    decimal CurrentMonthlyCost,
    decimal ProjectedAnnualCost,
    string Currency,
    int ActiveLicenses,
    int ActiveServers,
    int TotalContractedLicenses,
    int TotalUsedLicenses,
    int IdleLicenses,
    int ContractsExpiringIn30Days,
    int ContractsExpiringIn60Days,
    int ContractsExpiringIn90Days,
    int OpenAlerts,
    int CriticalAlerts,
    IReadOnlyList<CostByCategoryDto> CostsByCategory,
    IReadOnlyList<TopCostDto> TopCosts,
    IReadOnlyList<ServersByEnvironmentDto> ServersByEnvironment,
    IReadOnlyList<TimelineEventDto> LatestEvents);

public sealed record CostByCategoryDto(string Category, decimal MonthlyAmount, decimal AnnualAmount);

public sealed record TopCostDto(
    Guid AssetId,
    string Name,
    string Kind,
    decimal MonthlyAmount,
    decimal AnnualAmount);

public sealed record ServersByEnvironmentDto(string Environment, int Count, decimal MonthlyAmount);
