using FinOS.Analytics.Application.DTOs;
using FinOS.Analytics.Application.Queries;
using FinOS.Common.Exceptions;
using FinOS.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.Analytics.API.Controllers;

[ApiController]
[Route("api/cash-flow")]
[Authorize]
public sealed class CashFlowController(IMediator mediator) : AuthenticatedControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<CashFlowAnalyticsDto>>> Get(
        [FromQuery] int months = 12,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var endExclusive = (endDate ?? DateTime.UtcNow.Date).Date.AddDays(1);
        var start = startDate?.Date ?? new DateTime(endExclusive.Year, endExclusive.Month, 1).AddMonths(1 - months);
        if (months is < 1 or > 60 || start >= endExclusive || endExclusive > DateTime.UtcNow.Date.AddDays(2) ||
            endExclusive > start.AddMonths(61))
            throw new ValidationException("range", "Select a valid cash-flow range of up to 60 months ending no later than today.");
        var result = await mediator.Send(
            new GetCashFlowAnalyticsQuery(AuthenticatedUserId, start, endExclusive), cancellationToken);
        return Ok(ApiResponse<CashFlowAnalyticsDto>.Ok(result));
    }
}
