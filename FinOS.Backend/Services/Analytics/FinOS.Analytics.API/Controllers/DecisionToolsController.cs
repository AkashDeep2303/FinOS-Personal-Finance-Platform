using FinOS.Analytics.Application.DTOs;
using FinOS.Analytics.Application.Queries;
using FinOS.Analytics.Application.Commands;
using FinOS.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.Analytics.API.Controllers;

[ApiController]
[Route("api/decision-tools")]
[Authorize]
public class DecisionToolsController : AuthenticatedControllerBase
{
    private readonly IMediator _mediator;
    public DecisionToolsController(IMediator mediator) => _mediator = mediator;

    [HttpPost("calculate")]
    public async Task<ActionResult<ApiResponse<CalculatorResultDto>>> Calculate(CalculatorRequest request)
    {
        _ = AuthenticatedUserId;
        return Ok(ApiResponse<CalculatorResultDto>.Ok(await _mediator.Send(new CalculateFinancialToolQuery(request))));
    }

    [HttpPost("xirr")]
    public async Task<ActionResult<ApiResponse<CalculatorResultDto>>> Xirr(XirrRequest request)
    {
        _ = AuthenticatedUserId;
        return Ok(ApiResponse<CalculatorResultDto>.Ok(await _mediator.Send(new CalculateXirrQuery(request))));
    }

    [HttpPost("scenario")]
    public async Task<ActionResult<ApiResponse<ScenarioResultDto>>> Scenario(ScenarioRequest request)
    {
        _ = AuthenticatedUserId;
        return Ok(ApiResponse<ScenarioResultDto>.Ok(await _mediator.Send(new CalculateScenarioQuery(request))));
    }

    [HttpGet("scenarios")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SavedScenarioDto>>>> SavedScenarios() =>
        Ok(ApiResponse<IReadOnlyList<SavedScenarioDto>>.Ok(
            await _mediator.Send(new GetSavedScenariosQuery(AuthenticatedUserId))));

    [HttpPost("scenarios")]
    public async Task<ActionResult<ApiResponse<SavedScenarioDto>>> SaveScenario(SaveScenarioRequest request) =>
        Ok(ApiResponse<SavedScenarioDto>.Ok(
            await _mediator.Send(new SaveScenarioCommand(AuthenticatedUserId, request)), "Scenario saved"));

    [HttpDelete("scenarios/{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteScenario(long id)
    {
        await _mediator.Send(new DeleteScenarioCommand(AuthenticatedUserId, id));
        return Ok(ApiResponse<object>.Ok(new { }, "Scenario deleted"));
    }
}
