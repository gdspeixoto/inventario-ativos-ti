using ControleAtivos.Application.Common;
using ControleAtivos.Domain.Enums;

namespace ControleAtivos.Application.Features.Costs;

/// <summary>Linha do historico consolidado de reajustes.</summary>
public sealed record PriceAdjustmentListItemDto(
    Guid Id,
    PriceAdjustmentTargetType TargetType,
    Guid TargetId,
    string TargetName,
    decimal PreviousAmount,
    decimal NewAmount,
    decimal AbsoluteDifference,
    decimal PercentageDifference,
    decimal AnnualImpact,
    string Currency,
    DateOnly EffectiveDate,
    string Reason,
    AdjustmentIndex IndexApplied,
    string? CreatedByName,
    string? ApprovedByName,
    string OrganizationalUnitName,
    bool ExceedsThreshold);

public sealed record ListPriceAdjustmentsQuery : PageRequest
{
    public PriceAdjustmentTargetType? TargetType { get; init; }

    public Guid? TargetId { get; init; }

    public Guid? OrganizationalUnitId { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    /// <summary>Somente reajustes acima do limite percentual configurado.</summary>
    public bool? OnlyAboveThreshold { get; init; }

    /// <summary>Somente aumentos (exclui reducoes negociadas).</summary>
    public bool? OnlyIncreases { get; init; }
}

/// <summary>Consolidado financeiro exibido na tela de custos.</summary>
public sealed record CostSummaryDto(
    decimal CurrentMonthlyCost,
    decimal ProjectedAnnualCost,
    string Currency,
    decimal AdjustedThisMonth,
    decimal AccumulatedMonthlyImpact,
    decimal AccumulatedAnnualImpact,
    decimal? HighestPercentageIncrease,
    decimal? HighestAbsoluteIncrease,
    int AssetsWithoutAdjustmentOver12Months,
    int AdjustmentsInPeriod,
    IReadOnlyList<CostByGroupDto> ByCategory,
    IReadOnlyList<CostByGroupDto> BySupplier,
    IReadOnlyList<CostByGroupDto> ByOrganizationalUnit);

public sealed record CostByGroupDto(
    string Label,
    Guid? Id,
    decimal MonthlyAmount,
    decimal AnnualAmount,
    int ItemCount);

/// <summary>
/// Lancamento de custo realizado.
///
/// Complementa o valor recorrente do ativo: o ativo diz quanto <em>deveria</em>
/// custar por mes, e o lancamento registra o que de fato foi gasto numa
/// competencia — inclusive despesas pontuais que nao pertencem a nenhum ativo.
/// </summary>
public sealed record CreateCostEntryCommand
{
    public required Guid OrganizationalUnitId { get; init; }

    public required CostType Type { get; init; }

    public required CostCategory Category { get; init; }

    public required decimal Amount { get; init; }

    /// <summary>Mes de competencia. O dia informado e normalizado para o dia 1.</summary>
    public required DateOnly CompetenceMonth { get; init; }

    public required string Description { get; init; }

    public string Currency { get; init; } = "BRL";

    /// <summary>
    /// Vinculo opcional com um ativo. Quando informado, fornecedor, contrato e
    /// centro de custo sao herdados dele — evita que a mesma despesa apareca
    /// classificada de forma diferente conforme quem lancou.
    /// </summary>
    public Guid? AssetId { get; init; }

    public Guid? ContractId { get; init; }
}

public sealed record CostEntryDto(
    Guid Id,
    Guid OrganizationalUnitId,
    string OrganizationalUnitName,
    Guid? AssetId,
    string? AssetName,
    Guid? ContractId,
    string? ContractNumber,
    Guid? SupplierId,
    string? SupplierName,
    CostType Type,
    CostCategory Category,
    decimal Amount,
    string Currency,
    DateOnly CompetenceMonth,
    DateOnly EffectiveDate,
    string Description,
    DateTimeOffset CreatedAt);

public sealed record ListCostEntriesQuery : PageRequest
{
    public Guid? OrganizationalUnitId { get; init; }

    public Guid? AssetId { get; init; }

    public Guid? ContractId { get; init; }

    public CostCategory? Category { get; init; }

    public CostType? Type { get; init; }

    /// <summary>Filtra pela competencia, inclusive.</summary>
    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }
}
