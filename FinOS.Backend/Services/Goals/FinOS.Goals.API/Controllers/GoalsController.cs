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
public class GoalsController : AuthenticatedControllerBase
{
    private readonly IMediator _mediator;

    public GoalsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("me")]
    public async Task<ApiResponse<List<GoalDto>>> GetMine()
    {
        var goals = await _mediator.Send(new GetGoalsByUserQuery(AuthenticatedUserId));
        return ApiResponse<List<GoalDto>>.Ok(goals);
    }

    [Obsolete("Use GET api/goals/me.")]
    [HttpGet("user/{userId:long}")]
    public async Task<ApiResponse<List<GoalDto>>> GetGoalsByUser(long userId)
    {
        if (userId != AuthenticatedUserId)
            throw new UnauthorizedAccessException("The route user does not match the authenticated user.");
        var goals = await _mediator.Send(new GetGoalsByUserQuery(AuthenticatedUserId));
        return ApiResponse<List<GoalDto>>.Ok(goals);
    }

    [HttpGet("{goalId}/progress")]
    public async Task<ApiResponse<GoalProgressDto>> GetGoalProgress(long goalId)
    {
        var progress = await _mediator.Send(new GetGoalProgressQuery(AuthenticatedUserId, goalId));
        return ApiResponse<GoalProgressDto>.Ok(progress);
    }

    [HttpPost]
    public async Task<ApiResponse<GoalDto>> CreateGoal([FromBody] CreateGoalDto dto)
    {
        var goal = await _mediator.Send(new CreateGoalCommand(dto with { UserId = AuthenticatedUserId }));
        return ApiResponse<GoalDto>.Ok(goal, "Goal created successfully");
    }

    [HttpDelete("{goalId}")]
    public async Task<ApiResponse<Unit>> DeleteGoal(long goalId)
    {
        await _mediator.Send(new DeleteGoalCommand(AuthenticatedUserId, goalId));
        return ApiResponse<Unit>.Ok(Unit.Value, "Goal deleted successfully");
    }
    [HttpPut]
    public async Task<ApiResponse<GoalDto>> UpdateGoal([FromBody] UpdateGoalDto dto)
    {
        var goal = await _mediator.Send(new UpdateGoalCommand(AuthenticatedUserId, dto));
        return ApiResponse<GoalDto>.Ok(goal, "Goal updated successfully");
    }

    [HttpPost("{goalId}/contribute")]
    public async Task<ApiResponse<GoalContributionDto>> AddContribution(long goalId, [FromBody] AddGoalContributionDto dto)
    {
        var contribution = await _mediator.Send(new AddGoalContributionCommand(
            AuthenticatedUserId, dto with { GoalId = goalId }));
        return ApiResponse<GoalContributionDto>.Ok(contribution, "Contribution added successfully");
    }

    [HttpPost("{goalId}/pause")]
    public async Task<ApiResponse<GoalDto>> PauseGoal(long goalId)
    {
        var goal = await _mediator.Send(new PauseGoalCommand(AuthenticatedUserId, goalId));
        return ApiResponse<GoalDto>.Ok(goal, "Goal paused successfully");
    }

    [HttpPost("{goalId}/resume")]
    public async Task<ApiResponse<GoalDto>> ResumeGoal(long goalId)
    {
        var goal = await _mediator.Send(new ResumeGoalCommand(AuthenticatedUserId, goalId));
        return ApiResponse<GoalDto>.Ok(goal, "Goal resumed successfully");
    }
}
