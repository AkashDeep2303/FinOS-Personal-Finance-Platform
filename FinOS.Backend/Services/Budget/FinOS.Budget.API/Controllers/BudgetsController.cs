using FinOS.Budget.Application.Commands;
using FinOS.Budget.Application.DTOs;
using FinOS.Budget.Application.Queries;
using FinOS.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.Budget.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BudgetsController : AuthenticatedControllerBase
{
    private readonly IMediator _mediator;

    public BudgetsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<List<BudgetListDto>>>> GetMine([FromQuery] bool? isActive = null)
    {
        var result = await _mediator.Send(new GetBudgetsByUserQuery(AuthenticatedUserId, isActive));
        return Ok(ApiResponse<List<BudgetListDto>>.Ok(result));
    }

    [Obsolete("Use GET api/budgets/me.")]
    [HttpGet("user/{userId:long}")]
    public async Task<ActionResult<ApiResponse<List<BudgetListDto>>>> GetByUser(long userId, [FromQuery] bool? isActive = null)
    {
        if (userId != AuthenticatedUserId) return Forbid();
        var result = await _mediator.Send(new GetBudgetsByUserQuery(AuthenticatedUserId, isActive));
        return Ok(ApiResponse<List<BudgetListDto>>.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<BudgetDto>>> GetById(long id)
    {
        // Use GetBudgetVsActual to get full details with spent calculation
        var result = await _mediator.Send(new GetBudgetVsActualQuery(AuthenticatedUserId, id));
        return Ok(ApiResponse<BudgetVsActualDto>.Ok(result));
    }

    [HttpGet("{budgetId}/vs-actual")]
    public async Task<ActionResult<ApiResponse<BudgetVsActualDto>>> GetVsActual(long budgetId)
    {
        var result = await _mediator.Send(new GetBudgetVsActualQuery(AuthenticatedUserId, budgetId));
        return Ok(ApiResponse<BudgetVsActualDto>.Ok(result));
    }

    [HttpGet("{budgetId}/alerts")]
    public async Task<ActionResult<ApiResponse<List<BudgetAlertDto>>>> GetAlerts(long budgetId, [FromQuery] bool? unreadOnly = null)
    {
        var result = await _mediator.Send(new GetBudgetAlertsQuery(AuthenticatedUserId, budgetId, unreadOnly));
        return Ok(ApiResponse<List<BudgetAlertDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<BudgetDto>>> Create([FromBody] CreateBudgetRequest request)
    {
        request.UserId = AuthenticatedUserId;
        var result = await _mediator.Send(new CreateBudgetCommand(request));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<BudgetDto>.Ok(result, "Budget created successfully"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<BudgetDto>>> Update(long id, [FromBody] UpdateBudgetRequest request)
    {
        var result = await _mediator.Send(new UpdateBudgetCommand(AuthenticatedUserId, id, request));
        return Ok(ApiResponse<BudgetDto>.Ok(result, "Budget updated successfully"));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<Unit>>> Delete(long id)
    {
        await _mediator.Send(new DeleteBudgetCommand(AuthenticatedUserId, id));
        return Ok(ApiResponse<Unit>.Ok(Unit.Value, "Budget deleted successfully"));
    }

    [HttpPut("{budgetId}/spent")]
    public async Task<ActionResult<ApiResponse<Unit>>> UpdateSpent(long budgetId, [FromBody] List<CategorySpentUpdate> updates)
    {
        await _mediator.Send(new UpdateBudgetSpentCommand(AuthenticatedUserId, budgetId, updates));
        return Ok(ApiResponse<Unit>.Ok(Unit.Value, "Budget spent updated successfully"));
    }

    [HttpPost("{budgetId}/check-alerts")]
    public async Task<ActionResult<ApiResponse<List<BudgetAlertDto>>>> CheckAlerts(long budgetId)
    {
        var result = await _mediator.Send(new CheckBudgetAlertsCommand(AuthenticatedUserId, budgetId));
        return Ok(ApiResponse<List<BudgetAlertDto>>.Ok(result.Select(a => new BudgetAlertDto
        {
            Id = a.Id,
            BudgetCategoryId = a.BudgetCategoryId,
            AlertType = a.AlertType,
            AlertTypeDisplay = a.AlertType.ToString(),
            ThresholdPercentage = a.ThresholdPercentage,
            Message = a.Message,
            IsRead = a.IsRead,
            CreatedAt = a.CreatedAt
        }).ToList()));
    }
}
