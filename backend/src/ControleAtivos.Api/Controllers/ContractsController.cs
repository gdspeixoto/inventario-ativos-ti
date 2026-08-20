using ControleAtivos.Application.Abstractions;
using ControleAtivos.Application.Common;
using ControleAtivos.Application.Features.Contracts;
using ControleAtivos.Application.Features.Licenses;
using ControleAtivos.Application.Features.Suppliers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ControleAtivos.Api.Controllers;

[ApiController]
[Route("api/v1/contracts")]
[Produces("application/json")]
public sealed class ContractsController(ContractService contracts) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.ContractsRead)]
    [ProducesResponseType<PagedResult<ContractListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ContractListItemDto>>> List(
        [FromQuery] ListContractsQuery query,
        CancellationToken cancellationToken) =>
        Ok(await contracts.ListAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.ContractsRead)]
    [ProducesResponseType<ContractDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContractDetailDto>> Get(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await contracts.GetAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Permissions.ContractsWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateContractCommand command,
        CancellationToken cancellationToken)
    {
        var id = await contracts.CreateAsync(command, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }

    /// <summary>
    /// Exclui o contrato. Para registrar o fim da vigencia de um contrato que
    /// valeu, use o encerramento.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.ContractsWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await contracts.DeleteAsync(id, cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/renew")]
    [Authorize(Permissions.ContractsWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Renew(
        Guid id,
        [FromBody] RenewContractCommand command,
        CancellationToken cancellationToken)
    {
        await contracts.RenewAsync(id, command, cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/price-adjustments")]
    [Authorize(Permissions.CostsWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType<PriceAdjustmentResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PriceAdjustmentResultDto>> ApplyPriceAdjustment(
        Guid id,
        [FromBody] ApplyPriceAdjustmentCommand command,
        CancellationToken cancellationToken) =>
        Ok(await contracts.ApplyPriceAdjustmentAsync(id, command, cancellationToken));

    [HttpPost("{id:guid}/terminate")]
    [Authorize(Permissions.ContractsWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Terminate(
        Guid id,
        [FromBody] TerminateContractCommand command,
        CancellationToken cancellationToken)
    {
        await contracts.TerminateAsync(id, command, cancellationToken);

        return NoContent();
    }
}

[ApiController]
[Route("api/v1/suppliers")]
[Produces("application/json")]
public sealed class SuppliersController(SupplierService suppliers) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.SuppliersRead)]
    [ProducesResponseType<PagedResult<SupplierListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SupplierListItemDto>>> List(
        [FromQuery] ListSuppliersQuery query,
        CancellationToken cancellationToken) =>
        Ok(await suppliers.ListAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.SuppliersRead)]
    [ProducesResponseType<SupplierDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierDetailDto>> Get(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await suppliers.GetAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Permissions.SuppliersWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateSupplierCommand command,
        CancellationToken cancellationToken)
    {
        var id = await suppliers.CreateAsync(command, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }

    /// <summary>Corrige a razao social, quando ela foi cadastrada errada.</summary>
    [HttpPut("{id:guid}/name")]
    [Authorize(Permissions.SuppliersWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Rename(
        Guid id,
        [FromBody] RenameSupplierRequest request,
        CancellationToken cancellationToken)
    {
        await suppliers.RenameAsync(id, request.Name, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Exclui o fornecedor. Havendo vinculo, marque como inativo: assim os
    /// registros historicos continuam sabendo de quem se tratava.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.SuppliersWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await suppliers.DeleteAsync(id, cancellationToken);

        return NoContent();
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.SuppliersWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateSupplierCommand command,
        CancellationToken cancellationToken)
    {
        await suppliers.UpdateAsync(id, command, cancellationToken);

        return NoContent();
    }
}

/// <summary>Corpo da correcao de razao social.</summary>
public sealed record RenameSupplierRequest(string Name);
