using FinOS.Common.Models;
using FinOS.AIAssistant.Application.Commands;
using FinOS.AIAssistant.Application.DTOs;
using FinOS.AIAssistant.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.AIAssistant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMediator _mediator;

    public MessagesController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<MessageResponseDto>>> Send([FromBody] SendMessageDto dto)
    {
        var result = await _mediator.Send(new SendMessageCommand(dto));
        return Ok(ApiResponse<MessageResponseDto>.Ok(result, "Message sent successfully"));
    }

    [HttpGet("recent")]
    public async Task<ActionResult<ApiResponse<List<MessageDto>>>> GetRecent([FromQuery] long userId, [FromQuery] int count = 10)
    {
        var result = await _mediator.Send(new GetRecentQueriesQuery(userId, count));
        return Ok(ApiResponse<List<MessageDto>>.Ok(result));
    }
}
