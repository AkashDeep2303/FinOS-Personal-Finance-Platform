using FinOS.Common.Models;
using FinOS.Notification.Application.Commands;
using FinOS.Notification.Application.DTOs;
using FinOS.Notification.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinOS.Notification.API.Controllers;

/// <summary>
/// Manages user notifications: list, create, mark-read, mark-all-read, unread-count,
/// and process scheduled notifications.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get paginated, filtered notifications for a user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<NotificationDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<NotificationDto>>>> GetNotifications(
        [FromQuery] long userId,
        [FromQuery] bool? isRead,
        [FromQuery] int? notificationTypeId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var paging = new PagedQuery { PageNumber = pageNumber, PageSize = pageSize };
        var result = await _mediator.Send(new GetNotificationsByUserQuery(userId, isRead, notificationTypeId, paging));
        return Ok(ApiResponse<PagedResult<NotificationDto>>.Ok(result));
    }

    /// <summary>
    /// Get the count of unread notifications for a user.
    /// </summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(ApiResponse<UnreadCountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UnreadCountDto>>> GetUnreadCount([FromQuery] long userId)
    {
        var result = await _mediator.Send(new GetUnreadCountQuery(userId));
        return Ok(ApiResponse<UnreadCountDto>.Ok(result));
    }

    /// <summary>
    /// Create a new notification.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<NotificationDto>>> Create([FromBody] CreateNotificationDto dto)
    {
        var result = await _mediator.Send(new CreateNotificationCommand(dto));
        return Ok(ApiResponse<NotificationDto>.Ok(result, "Notification created successfully"));
    }

    /// <summary>
    /// Mark a single notification as read.
    /// </summary>
    [HttpPut("{id}/read")]
    [ProducesResponseType(typeof(ApiResponse<Unit>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<Unit>>> MarkAsRead(long id, [FromQuery] long userId)
    {
        await _mediator.Send(new MarkAsReadCommand(id, userId));
        return Ok(ApiResponse<Unit>.Ok(Unit.Value, "Notification marked as read"));
    }

    /// <summary>
    /// Mark all unread notifications for a user as read.
    /// </summary>
    [HttpPut("read-all")]
    [ProducesResponseType(typeof(ApiResponse<Unit>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Unit>>> MarkAllAsRead([FromQuery] long userId)
    {
        await _mediator.Send(new MarkAllAsReadCommand(userId));
        return Ok(ApiResponse<Unit>.Ok(Unit.Value, "All notifications marked as read"));
    }

    /// <summary>
    /// Process all scheduled notifications that are due for delivery.
    /// Intended to be called by a scheduler / cron job.
    /// </summary>
    [HttpPost("process-scheduled")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<int>>> ProcessScheduled()
    {
        var count = await _mediator.Send(new ProcessScheduledNotificationsCommand());
        return Ok(ApiResponse<int>.Ok(count, $"Processed {count} scheduled notifications"));
    }
}
