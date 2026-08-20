using ControleAtivos.Application.Abstractions;
using ControleAtivos.Application.Common;
using ControleAtivos.Application.Features.Servers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ControleAtivos.Api.Controllers;

[ApiController]
[Route("api/v1/servers")]
[Produces("application/json")]
public sealed class ServersController(ServerService servers) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.AssetsRead)]
    [ProducesResponseType<PagedResult<ServerListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ServerListItemDto>>> List(
        [FromQuery] ListServersQuery query,
        CancellationToken cancellationToken) =>
        Ok(await servers.ListAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.AssetsRead)]
    [ProducesResponseType<ServerDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServerDetailDto>> Get(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await servers.GetAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Permissions.AssetsWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateServerCommand command,
        CancellationToken cancellationToken)
    {
        var id = await servers.CreateAsync(command, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }

    /// <summary>Atualiza a composicao de custos e recalcula o total mensal.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Permissions.AssetsWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateServerCommand command,
        CancellationToken cancellationToken)
    {
        await servers.UpdateAsync(id, command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Exclui o servidor. Para registrar que uma maquina real saiu de operacao,
    /// use a desativacao — ela preserva o historico.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.AssetsWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await servers.DeleteAsync(id, cancellationToken);

        return NoContent();
    }

    [HttpPut("{id:guid}/costs")]
    [Authorize(Permissions.CostsWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType<ServerCostChangeResultDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ServerCostChangeResultDto>> UpdateCosts(
        Guid id,
        [FromBody] UpdateServerCostsCommand command,
        CancellationToken cancellationToken) =>
        Ok(await servers.UpdateCostsAsync(id, command, cancellationToken));

    [HttpPost("{id:guid}/resize")]
    [Authorize(Permissions.AssetsWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Resize(
        Guid id,
        [FromBody] ResizeServerCommand command,
        CancellationToken cancellationToken)
    {
        await servers.ResizeAsync(id, command, cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/decommission")]
    [Authorize(Permissions.AssetsWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Decommission(
        Guid id,
        [FromBody] DecommissionServerCommand command,
        CancellationToken cancellationToken)
    {
        await servers.DecommissionAsync(id, command, cancellationToken);

        return NoContent();
    }
}
