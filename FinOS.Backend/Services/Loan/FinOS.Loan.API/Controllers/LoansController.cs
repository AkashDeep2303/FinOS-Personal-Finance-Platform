using FinOS.Loan.Application.Commands;
using FinOS.Loan.Application.DTOs;
using FinOS.Loan.Application.Queries;
using FinOS.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.Loan.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LoansController : ControllerBase
{
    private readonly IMediator _mediator;

    public LoansController(IMediator mediator) { _mediator = mediator; }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<ApiResponse<List<LoanListDto>>>> GetByUser(long userId, [FromQuery] bool? isActive = null)
    {
        var result = await _mediator.Send(new GetLoansByUserQuery(userId, isActive));
        return Ok(ApiResponse<List<LoanListDto>>.Ok(result));
    }

    [HttpGet("{id}/summary")]
    public async Task<ActionResult<ApiResponse<LoanSummaryDto>>> GetSummary(long id)
    {
        var result = await _mediator.Send(new GetLoanSummaryQuery(id));
        return Ok(ApiResponse<LoanSummaryDto>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<LoanDto>>> Create([FromBody] CreateLoanRequest request)
    {
        var result = await _mediator.Send(new CreateLoanCommand(request));
        return CreatedAtAction(nameof(GetSummary), new { id = result.Id }, ApiResponse<LoanDto>.Ok(result, "Loan created successfully"));
    }

    [HttpPost("{id}/close")]
    public async Task<ActionResult<ApiResponse<Unit>>> Close(long id)
    {
        await _mediator.Send(new CloseLoanCommand(id));
        return Ok(ApiResponse<Unit>.Ok(Unit.Value, "Loan closed successfully"));
    }
}
