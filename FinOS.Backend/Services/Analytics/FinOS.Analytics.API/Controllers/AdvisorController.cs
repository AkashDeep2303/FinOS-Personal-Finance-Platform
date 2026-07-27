using FinOS.Analytics.Application.DTOs;
using FinOS.Analytics.Application.Queries;
using FinOS.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.Analytics.API.Controllers;

[ApiController]
[Route("api/advisor")]
[Authorize]
public class AdvisorController : AuthenticatedControllerBase
{
    private readonly IMediator _mediator;
    public AdvisorController(IMediator mediator) => _mediator = mediator;

    [HttpGet("opportunities")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FinancialInsightDto>>>> GetOpportunities()
    {
        var commandCenter = await _mediator.Send(new GetCommandCenterQuery(AuthenticatedUserId));
        return Ok(ApiResponse<IReadOnlyList<FinancialInsightDto>>.Ok(commandCenter.Insights));
    }
}
