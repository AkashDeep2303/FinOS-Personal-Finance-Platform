using FinOS.Common.Models;
using FinOS.Goals.Application.DTOs;
using FinOS.Goals.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.Goals.API.Controllers;

[ApiController]
[Route("api/goal-planning")]
[Authorize]
public class GoalPlanningController : AuthenticatedControllerBase
{
    private readonly IMediator _mediator;
    public GoalPlanningController(IMediator mediator) => _mediator = mediator;

    [HttpGet("funding-analysis")]
    public async Task<ActionResult<ApiResponse<GoalFundingAnalysisDto>>> GetFundingAnalysis(
        [FromQuery] decimal availableMonthlySurplus)
    {
        var result = await _mediator.Send(
            new GetGoalFundingAnalysisQuery(AuthenticatedUserId, availableMonthlySurplus));
        return Ok(ApiResponse<GoalFundingAnalysisDto>.Ok(result));
    }
}
