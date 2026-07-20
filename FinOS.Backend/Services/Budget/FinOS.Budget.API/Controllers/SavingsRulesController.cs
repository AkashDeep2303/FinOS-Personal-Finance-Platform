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
public class SavingsRulesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SavingsRulesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<ApiResponse<List<SavingsRuleDto>>>> GetByUser(long userId, [FromQuery] bool? isActive = null)
    {
        var result = await _mediator.Send(new GetSavingsRulesQuery(userId, isActive));
        return Ok(ApiResponse<List<SavingsRuleDto>>.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SavingsRuleDto>>> GetById(long id)
    {
        // Get from list and find - simplified for now
        return Ok(ApiResponse<SavingsRuleDto>.Ok(null!));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SavingsRuleDto>>> Create([FromBody] CreateSavingsRuleRequest request)
    {
        var result = await _mediator.Send(new CreateSavingsRuleCommand(request));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<SavingsRuleDto>.Ok(result, "Savings rule created successfully"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<SavingsRuleDto>>> Update(long id, [FromBody] UpdateSavingsRuleRequest request)
    {
        var result = await _mediator.Send(new UpdateSavingsRuleCommand(id, request));
        return Ok(ApiResponse<SavingsRuleDto>.Ok(result, "Savings rule updated successfully"));
    }
}
