using FinOS.Common.Models;
using FinOS.Loan.Application.DTOs;
using FinOS.Loan.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.Loan.API.Controllers;

[ApiController]
[Route("api/loan-strategy")]
[Authorize]
public class StrategyController : AuthenticatedControllerBase
{
    private readonly IMediator _mediator;
    public StrategyController(IMediator mediator) => _mediator = mediator;

    [HttpPost("compare")]
    public async Task<ActionResult<ApiResponse<LoanStrategyComparisonDto>>> Compare(
        [FromBody] CompareLoanStrategyRequest request)
    {
        var result = await _mediator.Send(new CompareLoanStrategyQuery(AuthenticatedUserId, request));
        return Ok(ApiResponse<LoanStrategyComparisonDto>.Ok(result));
    }
}
