using FinOS.Common.Models;
using FinOS.Goals.Application.Commands;
using FinOS.Goals.Application.DTOs;
using FinOS.Goals.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.Goals.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GoalsController : ControllerBase
{
    private readonly IMediator _mediator;

    public GoalsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("user/{userId}")]
    public async Task<ApiResponse<List<GoalDto>>> GetGoalsByUser(long userId)
    {
        var goals = await _mediator.Send(new GetGoalsByUserQuery(userId));
        return ApiResponse<List<GoalDto>>.Ok(goals);
    }

    [HttpGet("{goalId}/progress")]
    public async Task<ApiResponse<GoalProgressDto>> GetGoalProgress(long goalId)
    {
        var progress = await _mediator.Send(new GetGoalProgressQuery(goalId));
        return ApiResponse<GoalProgressDto>.Ok(progress);
    }

    [HttpPost]
    public async Task<ApiResponse<GoalDto>> CreateGoal([FromBody] CreateGoalDto dto)
    {
        var goal = await _mediator.Send(new CreateGoalCommand(dto));
        return ApiResponse<GoalDto>.Ok(goal, "Goal created successfully");
    }

    [HttpDelete("{goalId}")]
    public async Task<ApiResponse<Unit>> DeleteGoal(long goalId)
    {
        await _mediator.Send(new DeleteGoalCommand(goalId));
        return ApiResponse<Unit>.Ok(Unit.Value, "Goal deleted successfully");
    }
    [HttpPut]
    public async Task<ApiResponse<GoalDto>> UpdateGoal([FromBody] UpdateGoalDto dto)
    {
        var goal = await _mediator.Send(new UpdateGoalCommand(dto));
        return ApiResponse<GoalDto>.Ok(goal, "Goal updated successfully");
    }

    [HttpPost("{goalId}/contribute")]
    public async Task<ApiResponse<GoalContributionDto>> AddContribution(long goalId, [FromBody] AddGoalContributionDto dto)
    {
        var contribution = await _mediator.Send(new AddGoalContributionCommand(dto));
        return ApiResponse<GoalContributionDto>.Ok(contribution, "Contribution added successfully");
    }

    [HttpPost("{goalId}/pause")]
    public async Task<ApiResponse<GoalDto>> PauseGoal(long goalId)
    {
        var goal = await _mediator.Send(new PauseGoalCommand(goalId));
        return ApiResponse<GoalDto>.Ok(goal, "Goal paused successfully");
    }

    [HttpPost("{goalId}/resume")]
    public async Task<ApiResponse<GoalDto>> ResumeGoal(long goalId)
    {
        var goal = await _mediator.Send(new ResumeGoalCommand(goalId));
        return ApiResponse<GoalDto>.Ok(goal, "Goal resumed successfully");
    }
}
