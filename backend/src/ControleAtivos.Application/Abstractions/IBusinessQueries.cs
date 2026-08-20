using ControleAtivos.Application.Common;
using ControleAtivos.Application.Features.Alerts;
using ControleAtivos.Application.Features.Contracts;
using ControleAtivos.Application.Features.Costs;
using ControleAtivos.Application.Features.Reports;
using ControleAtivos.Application.Features.Suppliers;

namespace ControleAtivos.Application.Abstractions;

public interface IContractQueries
{
    Task<PagedResult<ContractListItemDto>> ListAsync(
        ListContractsQuery query,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default);

    Task<ContractDetailDto?> GetAsync(
        Guid id,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default);
}

public interface ISupplierQueries
{
    Task<PagedResult<SupplierListItemDto>> ListAsync(
        ListSuppliersQuery query,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default);

    Task<SupplierDetailDto?> GetAsync(
        Guid id,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default);
}

public interface ICostQueries
{
    Task<PagedResult<PriceAdjustmentListItemDto>> ListAdjustmentsAsync(
        ListPriceAdjustmentsQuery query,
        decimal thresholdPercentage,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default);

    Task<CostSummaryDto> GetSummaryAsync(
        decimal thresholdPercentage,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default);

    Task<PagedResult<CostEntryDto>> ListEntriesAsync(
        ListCostEntriesQuery query,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default);
}

public interface IAlertQueries
{
    Task<PagedResult<AlertDto>> ListAsync(
        ListAlertsQuery query,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default);
}

public interface IReportQueries
{
    Task<CostEvolutionReportDto> GetCostEvolutionAsync(
        ReportPeriodQuery query,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LicenseUtilizationReportDto>> GetLicenseUtilizationAsync(
        decimal utilizationBelow,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContractExpirationReportDto>> GetContractExpirationsAsync(
        int withinDays,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default);
}

public interface IAlertRepository
{
    Task<Domain.Entities.Alert?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> GetOpenDeduplicationKeysAsync(
        CancellationToken cancellationToken = default);

    void Add(Domain.Entities.Alert alert);
}

/// <summary>
/// Varredura que detecta pendencias e cria alertas. Implementada na camada de
/// persistencia porque depende de consultas agregadas sobre varias tabelas.
/// </summary>
public interface IAlertScanner
{
    Task<AlertScanResultDto> ScanAsync(
        AlertScanSettings settings,
        IOrganizationalScope scope,
        CancellationToken cancellationToken = default);
}
