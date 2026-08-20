using ControleAtivos.Application.Abstractions;
using ControleAtivos.Application.Common;
using ControleAtivos.Application.Features.Alerts;
using ControleAtivos.Application.Features.Costs;
using ControleAtivos.Application.Features.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ControleAtivos.Api.Controllers;

/// <summary>Consolidado financeiro e historico de reajustes.</summary>
[ApiController]
[Route("api/v1/costs")]
[Produces("application/json")]
public sealed class CostsController(CostService costs, CostEntryService entries) : ControllerBase
{
    [HttpGet("entries")]
    [Authorize(Permissions.CostsRead)]
    [ProducesResponseType<PagedResult<CostEntryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CostEntryDto>>> ListEntries(
        [FromQuery] ListCostEntriesQuery query,
        CancellationToken cancellationToken) =>
        Ok(await entries.ListAsync(query, cancellationToken));

    [HttpPost("entries")]
    [Authorize(Permissions.CostsWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateEntry(
        [FromBody] CreateCostEntryCommand command,
        CancellationToken cancellationToken)
    {
        var id = await entries.CreateAsync(command, cancellationToken);

        return CreatedAtAction(nameof(ListEntries), new { id }, new { id });
    }

    [HttpGet("summary")]
    [Authorize(Permissions.CostsRead)]
    [ProducesResponseType<CostSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CostSummaryDto>> Summary(CancellationToken cancellationToken) =>
        Ok(await costs.GetSummaryAsync(cancellationToken));

    [HttpGet("price-adjustments")]
    [Authorize(Permissions.CostsRead)]
    [ProducesResponseType<PagedResult<PriceAdjustmentListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PriceAdjustmentListItemDto>>> ListAdjustments(
        [FromQuery] ListPriceAdjustmentsQuery query,
        CancellationToken cancellationToken) =>
        Ok(await costs.ListAdjustmentsAsync(query, cancellationToken));
}

[ApiController]
[Route("api/v1/alerts")]
[Produces("application/json")]
public sealed class AlertsController(AlertService alerts) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.AlertsRead)]
    [ProducesResponseType<PagedResult<AlertDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AlertDto>>> List(
        [FromQuery] ListAlertsQuery query,
        CancellationToken cancellationToken) =>
        Ok(await alerts.ListAsync(query, cancellationToken));

    [HttpPost("{id:guid}/resolve")]
    [Authorize(Permissions.AlertsWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Resolve(
        Guid id,
        [FromBody] ResolveAlertCommand command,
        CancellationToken cancellationToken)
    {
        await alerts.ResolveAsync(id, command, cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/ignore")]
    [Authorize(Permissions.AlertsWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Ignore(
        Guid id,
        [FromBody] IgnoreAlertCommand command,
        CancellationToken cancellationToken)
    {
        await alerts.IgnoreAsync(id, command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Dispara a varredura de pendencias. Idempotente: enquanto um alerta
    /// continuar aberto, execucoes seguintes nao criam duplicatas.
    /// </summary>
    [HttpPost("scan")]
    [Authorize(Permissions.AlertsWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType<AlertScanResultDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AlertScanResultDto>> Scan(
        [FromBody] AlertScanSettings? settings,
        CancellationToken cancellationToken) =>
        Ok(await alerts.ScanAsync(settings, cancellationToken));
}

[ApiController]
[Route("api/v1/reports")]
[Produces("application/json")]
public sealed class ReportsController(ReportService reports) : ControllerBase
{
    [HttpGet("cost-evolution")]
    [Authorize(Permissions.ReportsRead)]
    [ProducesResponseType<CostEvolutionReportDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CostEvolutionReportDto>> CostEvolution(
        [FromQuery] ReportPeriodQuery query,
        CancellationToken cancellationToken) =>
        Ok(await reports.GetCostEvolutionAsync(query, cancellationToken));

    [HttpGet("license-utilization")]
    [Authorize(Permissions.ReportsRead)]
    [ProducesResponseType<IReadOnlyList<LicenseUtilizationReportDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LicenseUtilizationReportDto>>> LicenseUtilization(
        [FromQuery] decimal utilizationBelow,
        CancellationToken cancellationToken) =>
        Ok(await reports.GetLicenseUtilizationAsync(
            utilizationBelow <= 0 ? 100m : utilizationBelow,
            cancellationToken));

    [HttpGet("contract-expirations")]
    [Authorize(Permissions.ReportsRead)]
    [ProducesResponseType<IReadOnlyList<ContractExpirationReportDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ContractExpirationReportDto>>> ContractExpirations(
        [FromQuery] int withinDays,
        CancellationToken cancellationToken) =>
        Ok(await reports.GetContractExpirationsAsync(
            withinDays <= 0 ? 90 : withinDays,
            cancellationToken));
}
