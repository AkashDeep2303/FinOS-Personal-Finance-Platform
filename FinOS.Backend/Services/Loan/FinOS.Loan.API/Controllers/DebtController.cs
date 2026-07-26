using FinOS.Common.Models;
using FinOS.Loan.Application.DTOs;
using FinOS.Loan.Application.Queries;
using FinOS.Loan.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.Loan.API.Controllers;

[ApiController]
[Route("api/debt")]
[Authorize]
public class DebtController : AuthenticatedControllerBase
{
    private readonly IMediator _mediator;
    public DebtController(IMediator mediator) => _mediator = mediator;

    [HttpGet("overview")]
    public async Task<ActionResult<ApiResponse<DebtOverviewDto>>> GetOverview()
    {
        var result = await _mediator.Send(new GetDebtOverviewQuery(AuthenticatedUserId));
        return Ok(ApiResponse<DebtOverviewDto>.Ok(result));
    }

    [HttpGet("loans/{loanId:long}/rate-history")]
    public async Task<ActionResult<ApiResponse<List<LoanRateHistoryDto>>>> GetRateHistory(long loanId)
        => Ok(ApiResponse<List<LoanRateHistoryDto>>.Ok(
            await _mediator.Send(new GetLoanRateHistoryQuery(AuthenticatedUserId, loanId))));

    [HttpPost("loans/{loanId:long}/rate-history")]
    public async Task<ActionResult<ApiResponse<Unit>>> AddRateChange(long loanId, AddLoanRateChangeRequest request)
    {
        await _mediator.Send(new AddLoanRateChangeCommand(AuthenticatedUserId, loanId, request));
        return Ok(ApiResponse<Unit>.Ok(Unit.Value, "Interest rate change recorded"));
    }

    [HttpGet("loans/{loanId:long}/payment-analysis")]
    public async Task<ActionResult<ApiResponse<LoanPaymentAnalysisDto>>> GetPaymentAnalysis(long loanId)
        => Ok(ApiResponse<LoanPaymentAnalysisDto>.Ok(
            await _mediator.Send(new GetLoanPaymentAnalysisQuery(AuthenticatedUserId, loanId))));
}
