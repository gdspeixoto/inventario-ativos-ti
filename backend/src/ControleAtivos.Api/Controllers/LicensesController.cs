using ControleAtivos.Application.Abstractions;
using ControleAtivos.Application.Common;
using ControleAtivos.Application.Features.Licenses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ControleAtivos.Api.Controllers;

/// <summary>
/// Licencas de software.
///
/// Cada rota declara explicitamente a permissao exigida, de modo que a ausencia
/// de autorizacao seja visivel na leitura do codigo. As de escrita usam a cota
/// mais restritiva de rate limiting.
/// </summary>
[ApiController]
[Route("api/v1/licenses")]
[Produces("application/json")]
public sealed class LicensesController(LicenseService licenses) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.AssetsRead)]
    [ProducesResponseType<PagedResult<LicenseListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LicenseListItemDto>>> List(
        [FromQuery] ListLicensesQuery query,
        CancellationToken cancellationToken) =>
        Ok(await licenses.ListAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.AssetsRead)]
    [ProducesResponseType<LicenseDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LicenseDetailDto>> Get(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await licenses.GetAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Permissions.AssetsWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateLicenseCommand command,
        CancellationToken cancellationToken)
    {
        var id = await licenses.CreateAsync(command, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }

    /// <summary>
    /// Registra um reajuste. O corpo traz apenas o valor novo: valor anterior,
    /// diferenca e percentual sao calculados pelo servidor.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Permissions.AssetsWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateLicenseCommand command,
        CancellationToken cancellationToken)
    {
        await licenses.UpdateAsync(id, command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Cancela a licenca preservando o historico. E a alternativa oferecida
    /// quando a exclusao e recusada.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Permissions.AssetsWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromBody] CancelLicenseCommand command,
        CancellationToken cancellationToken)
    {
        await licenses.CancelAsync(id, command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Exclui a licenca. So e aceito enquanto nao houver historico financeiro;
    /// para encerrar o uso de uma licenca real, altere a situacao.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.AssetsWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await licenses.DeleteAsync(id, cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/price-adjustments")]
    [Authorize(Permissions.CostsWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType<PriceAdjustmentResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PriceAdjustmentResultDto>> ApplyPriceAdjustment(
        Guid id,
        [FromBody] ApplyPriceAdjustmentCommand command,
        CancellationToken cancellationToken) =>
        Ok(await licenses.ApplyPriceAdjustmentAsync(id, command, cancellationToken));

    [HttpPost("{id:guid}/quantity-changes")]
    [Authorize(Permissions.AssetsWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType<QuantityChangeResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<QuantityChangeResultDto>> ChangeQuantity(
        Guid id,
        [FromBody] ChangeLicenseQuantityCommand command,
        CancellationToken cancellationToken) =>
        Ok(await licenses.ChangeQuantityAsync(id, command, cancellationToken));

    [HttpPut("{id:guid}/usage")]
    [Authorize(Permissions.AssetsWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateUsage(
        Guid id,
        [FromBody] UpdateLicenseUsageCommand command,
        CancellationToken cancellationToken)
    {
        await licenses.UpdateUsageAsync(id, command, cancellationToken);

        return NoContent();
    }

    /// <summary>Simula um reajuste sem persistir nada.</summary>
    [HttpPost("simulate-adjustment")]
    [Authorize(Permissions.CostsRead)]
    [ProducesResponseType<SimulationResultDto>(StatusCodes.Status200OK)]
    public ActionResult<SimulationResultDto> Simulate([FromBody] SimulateAdjustmentQuery query) =>
        Ok(LicenseService.Simulate(query));
}
