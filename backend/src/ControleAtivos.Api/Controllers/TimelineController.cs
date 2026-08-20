using ControleAtivos.Application.Abstractions;
using ControleAtivos.Application.Common;
using ControleAtivos.Application.Features.Dashboard;
using ControleAtivos.Application.Features.Timeline;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControleAtivos.Api.Controllers;

/// <summary>Historico de eventos, geral ou por item.</summary>
[ApiController]
[Route("api/v1/timeline")]
[Produces("application/json")]
public sealed class TimelineController(TimelineService timeline) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.AssetsRead)]
    [ProducesResponseType<PagedResult<TimelineEventDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TimelineEventDto>>> List(
        [FromQuery] ListTimelineQuery query,
        CancellationToken cancellationToken) =>
        Ok(await timeline.ListAsync(query, cancellationToken));
}

[ApiController]
[Route("api/v1/dashboard")]
[Produces("application/json")]
public sealed class DashboardController(DashboardService dashboard) : ControllerBase
{
    [HttpGet("summary")]
    [Authorize(Permissions.AssetsRead)]
    [ProducesResponseType<DashboardSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryDto>> Summary(CancellationToken cancellationToken) =>
        Ok(await dashboard.GetSummaryAsync(cancellationToken));
}
