using FinOS.Common.Models;
using FinOS.Analytics.Application.DTOs;
using FinOS.Analytics.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.Analytics.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SpendingController : AuthenticatedControllerBase
{
    private readonly IMediator _mediator;

    public SpendingController(IMediator mediator) => _mediator = mediator;

    [HttpGet("trends")]
    public async Task<ActionResult<ApiResponse<List<SpendingTrendDto>>>> GetTrends([FromQuery] int months = 6)
    {
        var result = await _mediator.Send(new GetSpendingTrendsQuery(AuthenticatedUserId, months));
        return Ok(ApiResponse<List<SpendingTrendDto>>.Ok(result));
    }

    [HttpGet("income-vs-expense")]
    public async Task<ActionResult<ApiResponse<List<IncomeVsExpenseDto>>>> GetIncomeVsExpense([FromQuery] int months = 12)
    {
        var result = await _mediator.Send(new GetIncomeVsExpenseTrendQuery(AuthenticatedUserId, months));
        return Ok(ApiResponse<List<IncomeVsExpenseDto>>.Ok(result));
    }

    [HttpGet("category-breakdown")]
    public async Task<ActionResult<ApiResponse<List<CategoryBreakdownDto>>>> GetCategoryBreakdown([FromQuery] int yearMonth)
    {
        var result = await _mediator.Send(new GetCategoryWiseBreakdownQuery(AuthenticatedUserId, yearMonth));
        return Ok(ApiResponse<List<CategoryBreakdownDto>>.Ok(result));
    }
}
