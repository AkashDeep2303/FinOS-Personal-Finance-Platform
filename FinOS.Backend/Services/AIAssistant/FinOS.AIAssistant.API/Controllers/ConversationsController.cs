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
public class ConversationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConversationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ConversationDto>>>> GetConversations([FromQuery] long userId, [FromQuery] int count = 20)
    {
        var result = await _mediator.Send(new GetConversationsByUserQuery(userId, count));
        return Ok(ApiResponse<List<ConversationDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ConversationDto>>> Create([FromBody] CreateConversationDto dto)
    {
        var result = await _mediator.Send(new CreateConversationCommand(dto));
        return Ok(ApiResponse<ConversationDto>.Ok(result, "Conversation created successfully"));
    }

    [HttpGet("{id}/messages")]
    public async Task<ActionResult<ApiResponse<List<MessageDto>>>> GetMessages(long id, [FromQuery] long userId)
    {
        var result = await _mediator.Send(new GetConversationMessagesQuery(id, userId));
        return Ok(ApiResponse<List<MessageDto>>.Ok(result));
    }
}
