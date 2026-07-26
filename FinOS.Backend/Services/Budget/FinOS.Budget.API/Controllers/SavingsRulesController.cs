using FinOS.Budget.Application.Commands;
using FinOS.Budget.Application.DTOs;
using FinOS.Budget.Application.Queries;
using FinOS.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.Budget.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SavingsRulesController : AuthenticatedControllerBase
{
    private readonly IMediator _mediator;

    public SavingsRulesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<List<SavingsRuleDto>>>> GetMine([FromQuery] bool? isActive = null)
    {
        var result = await _mediator.Send(new GetSavingsRulesQuery(AuthenticatedUserId, isActive));
        return Ok(ApiResponse<List<SavingsRuleDto>>.Ok(result));
    }

    [Obsolete("Use GET api/savingsrules/me.")]
    [HttpGet("user/{userId:long}")]
    public async Task<ActionResult<ApiResponse<List<SavingsRuleDto>>>> GetByUser(long userId, [FromQuery] bool? isActive = null)
    {
        if (userId != AuthenticatedUserId) return Forbid();
        var result = await _mediator.Send(new GetSavingsRulesQuery(AuthenticatedUserId, isActive));
        return Ok(ApiResponse<List<SavingsRuleDto>>.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SavingsRuleDto>>> GetById(long id)
    {
        return Ok(ApiResponse<SavingsRuleDto>.Ok(
            await _mediator.Send(new GetSavingsRuleQuery(AuthenticatedUserId, id))));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SavingsRuleDto>>> Create([FromBody] CreateSavingsRuleRequest request)
    {
        var result = await _mediator.Send(new CreateSavingsRuleCommand(AuthenticatedUserId, request));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<SavingsRuleDto>.Ok(result, "Savings rule created successfully"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<SavingsRuleDto>>> Update(long id, [FromBody] UpdateSavingsRuleRequest request)
    {
        var result = await _mediator.Send(new UpdateSavingsRuleCommand(AuthenticatedUserId, id, request));
        return Ok(ApiResponse<SavingsRuleDto>.Ok(result, "Savings rule updated successfully"));
    }
}
