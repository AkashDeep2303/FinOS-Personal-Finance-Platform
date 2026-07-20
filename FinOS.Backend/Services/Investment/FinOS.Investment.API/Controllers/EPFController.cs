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
public class EPFController : ControllerBase
{
    private readonly IMediator _mediator;

    public EPFController(IMediator mediator) { _mediator = mediator; }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<EPFAccountDto>>> CreateAccount([FromBody] CreateEPFAccountRequest request)
    {
        // Would need CreateEPFAccountCommand - using placeholder
        return Ok(ApiResponse<EPFAccountDto>.Ok(null!, "EPF account creation endpoint"));
    }

    [HttpPost("contribution")]
    public async Task<ActionResult<ApiResponse<EPFContributionDto>>> AddContribution([FromBody] UpdateEPFContributionRequest request)
    {
        var result = await _mediator.Send(new UpdateEPFContributionCommand(request));
        return Ok(ApiResponse<EPFContributionDto>.Ok(result, "EPF contribution added successfully"));
    }

    [HttpGet("{epfAccountId}/statement")]
    public async Task<ActionResult<ApiResponse<List<EPFContributionDto>>>> GetStatement(long epfAccountId)
    {
        var result = await _mediator.Send(new GetEPFStatementQuery(epfAccountId));
        return Ok(ApiResponse<List<EPFContributionDto>>.Ok(result));
    }

    [HttpGet("{epfAccountId}/projection")]
    public async Task<ActionResult<ApiResponse<EPFProjectionDto>>> GetProjection(long epfAccountId, [FromQuery] int? retirementAge = null, [FromQuery] int? currentAge = null)
    {
        var result = await _mediator.Send(new GetEPFProjectionQuery(epfAccountId, retirementAge, currentAge));
        return Ok(ApiResponse<EPFProjectionDto>.Ok(result));
    }
}
