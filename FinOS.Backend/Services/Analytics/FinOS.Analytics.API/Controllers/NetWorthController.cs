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
public class NetWorthController : ControllerBase
{
    private readonly IMediator _mediator;

    public NetWorthController(IMediator mediator) => _mediator = mediator;

    [HttpGet("trend")]
    public async Task<ActionResult<ApiResponse<List<NetWorthDto>>>> GetTrend([FromQuery] long userId, [FromQuery] int months = 12)
    {
        var result = await _mediator.Send(new GetNetWorthTrendQuery(userId, months));
        return Ok(ApiResponse<List<NetWorthDto>>.Ok(result));
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<ApiResponse<NetWorthDto>>> Calculate([FromBody] CalculateNetWorthDto dto)
    {
        var result = await _mediator.Send(new CalculateNetWorthCommand(dto));
        return Ok(ApiResponse<NetWorthDto>.Ok(result, "Net worth calculated successfully"));
    }
}
