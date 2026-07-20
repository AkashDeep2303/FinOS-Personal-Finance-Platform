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
public class SpendingController : ControllerBase
{
    private readonly IMediator _mediator;

    public SpendingController(IMediator mediator) => _mediator = mediator;

    [HttpGet("trends")]
    public async Task<ActionResult<ApiResponse<List<SpendingTrendDto>>>> GetTrends([FromQuery] long userId, [FromQuery] int months = 6)
    {
        var result = await _mediator.Send(new GetSpendingTrendsQuery(userId, months));
        return Ok(ApiResponse<List<SpendingTrendDto>>.Ok(result));
    }

    [HttpGet("income-vs-expense")]
    public async Task<ActionResult<ApiResponse<List<IncomeVsExpenseDto>>>> GetIncomeVsExpense([FromQuery] long userId, [FromQuery] int months = 12)
    {
        var result = await _mediator.Send(new GetIncomeVsExpenseTrendQuery(userId, months));
        return Ok(ApiResponse<List<IncomeVsExpenseDto>>.Ok(result));
    }

    [HttpGet("category-breakdown")]
    public async Task<ActionResult<ApiResponse<List<CategoryBreakdownDto>>>> GetCategoryBreakdown([FromQuery] long userId, [FromQuery] int yearMonth)
    {
        var result = await _mediator.Send(new GetCategoryWiseBreakdownQuery(userId, yearMonth));
        return Ok(ApiResponse<List<CategoryBreakdownDto>>.Ok(result));
    }
}
