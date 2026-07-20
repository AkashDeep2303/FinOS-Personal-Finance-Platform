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
public class HoldingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public HoldingsController(IMediator mediator) { _mediator = mediator; }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<HoldingDto>>> GetById(long id)
    {
        var result = await _mediator.Send(new GetHoldingDetailsQuery(id));
        return Ok(ApiResponse<HoldingDto>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<HoldingDto>>> Create([FromBody] CreateHoldingRequest request)
    {
        var result = await _mediator.Send(new CreateHoldingCommand(request));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<HoldingDto>.Ok(result, "Holding created successfully"));
    }

    [HttpPut("{id}/price")]
    public async Task<ActionResult<ApiResponse<HoldingDto>>> UpdatePrice(long id, [FromBody] UpdateHoldingPriceRequest request)
    {
        var result = await _mediator.Send(new UpdateHoldingPriceCommand(id, request));
        return Ok(ApiResponse<HoldingDto>.Ok(result, "Holding price updated successfully"));
    }

    [HttpPost("transaction")]
    public async Task<ActionResult<ApiResponse<InvestmentTransactionDto>>> RecordTransaction([FromBody] RecordTransactionRequest request)
    {
        var result = await _mediator.Send(new RecordInvestmentTransactionCommand(request));
        return Ok(ApiResponse<InvestmentTransactionDto>.Ok(result, "Transaction recorded successfully"));
    }
}
