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
public class PrepaymentController : ControllerBase
{
    private readonly IMediator _mediator;

    public PrepaymentController(IMediator mediator) { _mediator = mediator; }

    [HttpPost("simulate")]
    public async Task<ActionResult<ApiResponse<PrepaymentSimulationDto>>> Simulate([FromBody] SimulatePrepaymentRequest request)
    {
        var result = await _mediator.Send(new SimulatePrepaymentCommand(request));
        return Ok(ApiResponse<PrepaymentSimulationDto>.Ok(result));
    }

    [HttpPost("execute")]
    public async Task<ActionResult<ApiResponse<LoanPrepaymentDto>>> Execute([FromBody] ExecutePrepaymentRequest request)
    {
        var result = await _mediator.Send(new ExecutePrepaymentCommand(request));
        return Ok(ApiResponse<LoanPrepaymentDto>.Ok(result, "Prepayment executed successfully"));
    }

    [HttpGet("loan/{loanId}/history")]
    public async Task<ActionResult<ApiResponse<List<LoanPrepaymentDto>>>> GetHistory(long loanId)
    {
        var result = await _mediator.Send(new GetPrepaymentHistoryQuery(loanId));
        return Ok(ApiResponse<List<LoanPrepaymentDto>>.Ok(result));
    }
}
