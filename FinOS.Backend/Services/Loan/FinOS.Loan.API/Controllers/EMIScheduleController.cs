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
public class EMIScheduleController : AuthenticatedControllerBase
{
    private readonly IMediator _mediator;

    public EMIScheduleController(IMediator mediator) { _mediator = mediator; }

    [HttpGet("loan/{loanId}")]
    public async Task<ActionResult<ApiResponse<List<EMIScheduleDto>>>> GetSchedule(long loanId)
    {
        var result = await _mediator.Send(new GetEMIScheduleQuery(AuthenticatedUserId, loanId));
        return Ok(ApiResponse<List<EMIScheduleDto>>.Ok(result));
    }

    [HttpGet("loan/{loanId}/upcoming")]
    public async Task<ActionResult<ApiResponse<List<EMIScheduleDto>>>> GetUpcoming(long loanId, [FromQuery] int count = 3)
    {
        var result = await _mediator.Send(new GetUpcomingEMIsQuery(AuthenticatedUserId, loanId, count));
        return Ok(ApiResponse<List<EMIScheduleDto>>.Ok(result));
    }

    [HttpPost("record-payment")]
    public async Task<ActionResult<ApiResponse<EMIScheduleDto>>> RecordPayment([FromBody] RecordEMIPaymentRequest request)
    {
        var result = await _mediator.Send(new RecordEMIPaymentCommand(AuthenticatedUserId, request));
        return Ok(ApiResponse<EMIScheduleDto>.Ok(result, "EMI payment recorded successfully"));
    }

    [HttpPost("loan/{loanId}/generate-schedule")]
    public async Task<ActionResult<ApiResponse<List<EMIScheduleDto>>>> GenerateSchedule(long loanId)
    {
        var result = await _mediator.Send(new GenerateAmortizationScheduleCommand(AuthenticatedUserId, loanId));
        return Ok(ApiResponse<List<EMIScheduleDto>>.Ok(result, "Amortization schedule generated"));
    }
}
