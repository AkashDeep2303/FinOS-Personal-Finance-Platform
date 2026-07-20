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
public class SIPsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SIPsController(IMediator mediator) { _mediator = mediator; }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<ApiResponse<List<SIPDto>>>> GetByUser(long userId, [FromQuery] bool? isActive = null)
    {
        var result = await _mediator.Send(new GetSIPListQuery(userId, isActive));
        return Ok(ApiResponse<List<SIPDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SIPDto>>> Create([FromBody] CreateSIPRequest request)
    {
        // Create SIP - would need a CreateSIPCommand
        return Ok(ApiResponse<SIPDto>.Ok(null!, "SIP creation endpoint"));
    }

    [HttpPost("process")]
    public async Task<ActionResult<ApiResponse<int>>> ProcessInstallments()
    {
        var count = await _mediator.Send(new ProcessSIPInstallmentsCommand());
        return Ok(ApiResponse<int>.Ok(count, $"Processed {count} SIP installments"));
    }
}
