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
public class LoansController : AuthenticatedControllerBase
{
    private readonly IMediator _mediator;

    public LoansController(IMediator mediator) { _mediator = mediator; }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<List<LoanListDto>>>> GetMine([FromQuery] bool? isActive = null)
    {
        var result = await _mediator.Send(new GetLoansByUserQuery(AuthenticatedUserId, isActive));
        return Ok(ApiResponse<List<LoanListDto>>.Ok(result));
    }

    [Obsolete("Use GET api/loans/me.")]
    [HttpGet("user/{userId:long}")]
    public async Task<ActionResult<ApiResponse<List<LoanListDto>>>> GetByUser(long userId, [FromQuery] bool? isActive = null)
    {
        if (userId != AuthenticatedUserId) return Forbid();
        var result = await _mediator.Send(new GetLoansByUserQuery(AuthenticatedUserId, isActive));
        return Ok(ApiResponse<List<LoanListDto>>.Ok(result));
    }

    [HttpGet("{id}/summary")]
    public async Task<ActionResult<ApiResponse<LoanSummaryDto>>> GetSummary(long id)
    {
        var result = await _mediator.Send(new GetLoanSummaryQuery(AuthenticatedUserId, id));
        return Ok(ApiResponse<LoanSummaryDto>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<LoanDto>>> Create([FromBody] CreateLoanRequest request)
    {
        request.UserId = AuthenticatedUserId;
        var result = await _mediator.Send(new CreateLoanCommand(request));
        return CreatedAtAction(nameof(GetSummary), new { id = result.Id }, ApiResponse<LoanDto>.Ok(result, "Loan created successfully"));
    }

    [HttpPost("{id}/close")]
    public async Task<ActionResult<ApiResponse<Unit>>> Close(long id)
    {
        await _mediator.Send(new CloseLoanCommand(AuthenticatedUserId, id));
        return Ok(ApiResponse<Unit>.Ok(Unit.Value, "Loan closed successfully"));
    }
}
