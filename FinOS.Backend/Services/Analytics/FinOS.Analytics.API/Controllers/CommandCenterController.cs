using FinOS.Analytics.Application.DTOs;
using FinOS.Analytics.Application.Queries;
using FinOS.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.Analytics.API.Controllers;

[ApiController]
[Route("api/command-center")]
[Authorize]
public class CommandCenterController : AuthenticatedControllerBase
{
    private readonly IMediator _mediator;

    public CommandCenterController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<CommandCenterDto>>> Get()
    {
        var result = await _mediator.Send(new GetCommandCenterQuery(AuthenticatedUserId));
        return Ok(ApiResponse<CommandCenterDto>.Ok(result));
    }
}
