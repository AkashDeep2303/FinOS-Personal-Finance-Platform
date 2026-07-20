using FinOS.Investment.Application.Commands;
using FinOS.Investment.Application.DTOs;
using FinOS.Investment.Application.Queries;
using FinOS.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.Investment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PortfoliosController : ControllerBase
{
    private readonly IMediator _mediator;

    public PortfoliosController(IMediator mediator) { _mediator = mediator; }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<ApiResponse<List<PortfolioListDto>>>> GetByUser(long userId)
    {
        var portfolios = await _mediator.Send(new GetPortfolioSummaryQuery(0)); // Placeholder
        return Ok(ApiResponse<List<PortfolioListDto>>.Ok(new List<PortfolioListDto>()));
    }

    [HttpGet("{id}/summary")]
    public async Task<ActionResult<ApiResponse<PortfolioSummaryDto>>> GetSummary(long id)
    {
        var result = await _mediator.Send(new GetPortfolioSummaryQuery(id));
        if (result == null) return NotFound(ApiResponse<PortfolioSummaryDto>.Fail("Portfolio not found"));
        return Ok(ApiResponse<PortfolioSummaryDto>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PortfolioDto>>> Create([FromBody] CreatePortfolioRequest request)
    {
        var result = await _mediator.Send(new CreatePortfolioCommand(request));
        return CreatedAtAction(nameof(GetSummary), new { id = result.Id }, ApiResponse<PortfolioDto>.Ok(result, "Portfolio created successfully"));
    }
}
