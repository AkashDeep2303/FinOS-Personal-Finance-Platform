using FinOS.Common.Models;
using FinOS.AIAssistant.Application.Commands;
using FinOS.AIAssistant.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.AIAssistant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FeedbackController : ControllerBase
{
    private readonly IMediator _mediator;

    public FeedbackController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Unit>>> Submit([FromBody] FeedbackDto dto)
    {
        await _mediator.Send(new SubmitFeedbackCommand(dto));
        return Ok(ApiResponse<Unit>.Ok(Unit.Value, "Feedback submitted successfully"));
    }

    [HttpGet("query-types")]
    public ActionResult<ApiResponse<List<QueryTypeDto>>> GetQueryTypes()
    {
        return Ok(ApiResponse<List<QueryTypeDto>>.Ok(QueryTypeCatalog.All));
    }
}
