using FinOS.Common.Models;
using FinOS.Investment.Application.DTOs;
using FinOS.Investment.Application.Queries;
using FinOS.Investment.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.Investment.API.Controllers;

[ApiController]
[Route("api/allocation")]
[Authorize]
public class AllocationController : AuthenticatedControllerBase
{
    private readonly IMediator _mediator;
    public AllocationController(IMediator mediator) => _mediator = mediator;

    [HttpPost("analyze")]
    public async Task<ActionResult<ApiResponse<AllocationAnalysisDto>>> Analyze(
        [FromBody] AllocationAnalysisRequest request)
    {
        var result = await _mediator.Send(new AnalyzeAllocationQuery(AuthenticatedUserId, request));
        return Ok(ApiResponse<AllocationAnalysisDto>.Ok(result));
    }

    [HttpGet("{portfolioId:long}/targets")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TargetAllocationInput>>>> GetTargets(long portfolioId)
        => Ok(ApiResponse<IReadOnlyList<TargetAllocationInput>>.Ok(
            await _mediator.Send(new GetTargetAllocationQuery(AuthenticatedUserId, portfolioId))));

    [HttpPut("{portfolioId:long}/targets")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TargetAllocationInput>>>> SaveTargets(
        long portfolioId, [FromBody] IReadOnlyList<TargetAllocationInput> targets)
        => Ok(ApiResponse<IReadOnlyList<TargetAllocationInput>>.Ok(
            await _mediator.Send(new SaveTargetAllocationCommand(
                AuthenticatedUserId, new AllocationAnalysisRequest(portfolioId, targets))),
            "Target allocation saved"));

    [HttpGet("{portfolioId:long}/performance")]
    public async Task<ActionResult<ApiResponse<InvestmentPerformanceDto>>> GetPerformance(
        long portfolioId, [FromQuery] int months = 12)
        => Ok(ApiResponse<InvestmentPerformanceDto>.Ok(
            await _mediator.Send(new GetInvestmentPerformanceQuery(AuthenticatedUserId, portfolioId, Math.Clamp(months, 1, 120)))));
}
