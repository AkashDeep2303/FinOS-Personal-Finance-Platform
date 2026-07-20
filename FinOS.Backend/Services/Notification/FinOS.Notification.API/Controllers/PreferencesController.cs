using FinOS.Common.Models;
using FinOS.Notification.Application.Commands;
using FinOS.Notification.Application.DTOs;
using FinOS.Notification.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.Notification.API.Controllers;

/// <summary>
/// Manages user notification preferences and notification type definitions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PreferencesController : ControllerBase
{
    private readonly IMediator _mediator;

    public PreferencesController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get all notification preferences for a user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<NotificationPreferenceDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<NotificationPreferenceDto>>>> GetPreferences([FromQuery] long userId)
    {
        var result = await _mediator.Send(new GetNotificationPreferencesQuery(userId));
        return Ok(ApiResponse<List<NotificationPreferenceDto>>.Ok(result));
    }

    /// <summary>
    /// Create or update a notification preference for a user.
    /// Uses partial-update semantics: only supplied fields are applied.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<NotificationPreferenceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<NotificationPreferenceDto>>> UpdatePreference(
        [FromBody] UpdateNotificationPreferenceDto dto)
    {
        var result = await _mediator.Send(new UpdateNotificationPreferenceCommand(dto));
        return Ok(ApiResponse<NotificationPreferenceDto>.Ok(result, "Preference updated successfully"));
    }

    /// <summary>
    /// Get all enabled notification types.
    /// </summary>
    [HttpGet("types")]
    [ProducesResponseType(typeof(ApiResponse<List<NotificationTypeDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<NotificationTypeDto>>>> GetNotificationTypes()
    {
        var result = await _mediator.Send(new GetNotificationTypesQuery());
        return Ok(ApiResponse<List<NotificationTypeDto>>.Ok(result));
    }
}
