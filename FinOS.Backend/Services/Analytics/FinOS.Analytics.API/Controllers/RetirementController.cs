using FinOS.Analytics.Application.DTOs;
using FinOS.Analytics.Application.Queries;
using FinOS.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.Analytics.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RetirementController : AuthenticatedControllerBase
{
    private readonly IMediator _mediator;
    public RetirementController(IMediator mediator) => _mediator = mediator;

    [HttpPost("project")]
    public async Task<ActionResult<ApiResponse<RetirementProjectionDto>>> Project(
        [FromBody] RetirementProjectionRequest request)
    {
        _ = AuthenticatedUserId;
        var result = await _mediator.Send(new CalculateRetirementProjectionQuery(request));
        return Ok(ApiResponse<RetirementProjectionDto>.Ok(result));
    }
}
