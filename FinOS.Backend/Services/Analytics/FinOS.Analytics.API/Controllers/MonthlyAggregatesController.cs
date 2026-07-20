using FinOS.Common.Models;
using FinOS.Analytics.Application.Commands;
using FinOS.Analytics.Application.DTOs;
using FinOS.Analytics.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.Analytics.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MonthlyAggregatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public MonthlyAggregatesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<MonthlyAggregateDto>>>> GetAggregates([FromQuery] long userId, [FromQuery] int months = 12)
    {
        var result = await _mediator.Send(new GetMonthlyAggregatesQuery(userId, months));
        return Ok(ApiResponse<List<MonthlyAggregateDto>>.Ok(result));
    }

    [HttpPost("generate")]
    public async Task<ActionResult<ApiResponse<MonthlyAggregateDto>>> Generate([FromBody] GenerateMonthlyAggregatesDto dto)
    {
        var result = await _mediator.Send(new GenerateMonthlyAggregatesCommand(dto));
        return Ok(ApiResponse<MonthlyAggregateDto>.Ok(result, "Monthly aggregates generated successfully"));
    }
}
