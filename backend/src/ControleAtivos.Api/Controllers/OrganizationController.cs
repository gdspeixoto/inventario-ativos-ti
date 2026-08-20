using ControleAtivos.Application.Abstractions;
using ControleAtivos.Application.Features.Organization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ControleAtivos.Api.Controllers;

/// <summary>
/// Estrutura organizacional e atribuicoes gerenciais.
///
/// A arvore retornada e recortada pelo escopo do usuario: um gerente de area
/// recebe a propria area como raiz, e nao a matriz inteira.
/// </summary>
[ApiController]
[Route("api/v1/organization")]
[Produces("application/json")]
public sealed class OrganizationController(OrganizationService organization) : ControllerBase
{
    [HttpGet("units")]
    [Authorize(Permissions.OrganizationRead)]
    [ProducesResponseType<IReadOnlyList<OrganizationalUnitTreeDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrganizationalUnitTreeDto>>> GetTree(
        CancellationToken cancellationToken) =>
        Ok(await organization.GetTreeAsync(cancellationToken));

    [HttpPost("units")]
    [Authorize(Permissions.OrganizationWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUnit(
        [FromBody] CreateOrganizationalUnitCommand command,
        CancellationToken cancellationToken)
    {
        var id = await organization.CreateUnitAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetTree), new { id }, new { id });
    }

    [HttpGet("units/{id:guid}")]
    [Authorize(Permissions.OrganizationRead)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<OrganizationalUnitDetailDto> GetUnit(Guid id, CancellationToken cancellationToken) =>
        organization.GetUnitAsync(id, cancellationToken);

    [HttpPut("units/{id:guid}")]
    [Authorize(Permissions.OrganizationWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUnit(
        Guid id,
        [FromBody] UpdateOrganizationalUnitCommand command,
        CancellationToken cancellationToken)
    {
        await organization.UpdateUnitAsync(id, command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Exclui a unidade. Existindo qualquer dependencia, a resposta 409 informa
    /// exatamente o que impede — e o caminho passa a ser desativar.
    /// </summary>
    [HttpDelete("units/{id:guid}")]
    [Authorize(Permissions.OrganizationWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteUnit(Guid id, CancellationToken cancellationToken)
    {
        await organization.DeleteUnitAsync(id, cancellationToken);

        return NoContent();
    }

    [HttpGet("management-assignments")]
    [Authorize(Permissions.OrganizationRead)]
    [ProducesResponseType<IReadOnlyList<ManagementAssignmentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ManagementAssignmentDto>>> ListAssignments(
        [FromQuery] Guid? userId,
        [FromQuery] Guid? organizationalUnitId,
        [FromQuery] bool onlyActive,
        CancellationToken cancellationToken) =>
        Ok(await organization.ListAssignmentsAsync(
            userId,
            organizationalUnitId,
            onlyActive,
            cancellationToken));

    /// <summary>
    /// Cria um vinculo de gestao. Vinculos anteriores permanecem ativos — e
    /// assim que um gerente responde por mais de uma area ou nucleo.
    /// </summary>
    [HttpPost("management-assignments")]
    [Authorize(Permissions.OrganizationWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAssignment(
        [FromBody] CreateManagementAssignmentCommand command,
        CancellationToken cancellationToken)
    {
        var id = await organization.CreateAssignmentAsync(command, cancellationToken);

        return CreatedAtAction(nameof(ListAssignments), new { id }, new { id });
    }

    [HttpPost("management-assignments/{id:guid}/finish")]
    [Authorize(Permissions.OrganizationWrite)]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> FinishAssignment(
        Guid id,
        [FromBody] FinishManagementAssignmentCommand command,
        CancellationToken cancellationToken)
    {
        await organization.FinishAssignmentAsync(id, command, cancellationToken);

        return NoContent();
    }
}
