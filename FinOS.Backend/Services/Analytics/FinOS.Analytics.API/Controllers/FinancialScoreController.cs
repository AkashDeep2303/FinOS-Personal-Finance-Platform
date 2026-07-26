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
public class FinancialScoreController : AuthenticatedControllerBase
{
    private readonly IMediator _mediator;

    public FinancialScoreController(IMediator mediator) => _mediator = mediator;

    [HttpGet("history")]
    public async Task<ActionResult<ApiResponse<List<FinancialScoreDto>>>> GetHistory([FromQuery] int months = 12)
    {
        var result = await _mediator.Send(new GetFinancialScoreHistoryQuery(AuthenticatedUserId, months));
        return Ok(ApiResponse<List<FinancialScoreDto>>.Ok(result));
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<ApiResponse<FinancialScoreDto>>> Calculate([FromBody] CalculateFinancialScoreDto dto)
    {
        var result = await _mediator.Send(new CalculateFinancialScoreCommand(dto with { UserId = AuthenticatedUserId }));
        return Ok(ApiResponse<FinancialScoreDto>.Ok(result, "Financial score calculated successfully"));
    }
}
