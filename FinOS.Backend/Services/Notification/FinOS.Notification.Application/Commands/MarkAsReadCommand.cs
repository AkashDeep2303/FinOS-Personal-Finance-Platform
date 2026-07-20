using FinOS.Common.Exceptions;
using FinOS.Notification.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FinOS.Notification.Application.Commands;

/// <summary>
/// Marks a single notification as read for a given user.
/// Dapper repos persist immediately — no SaveChangesAsync needed.
/// </summary>
public record MarkAsReadCommand(long NotificationId, long UserId) : IRequest<Unit>;

public class MarkAsReadCommandHandler : IRequestHandler<MarkAsReadCommand, Unit>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<MarkAsReadCommandHandler> _logger;

    public MarkAsReadCommandHandler(
        INotificationRepository notificationRepository,
        ILogger<MarkAsReadCommandHandler> logger)
    {
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(MarkAsReadCommand request, CancellationToken ct)
    {
        var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, ct)
            ?? throw new NotFoundException("Notification", request.NotificationId);

        if (notification.UserId != request.UserId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to mark notification {NotificationId} belonging to user {OwnerUserId}",
                request.UserId, request.NotificationId, notification.UserId);
            throw new UnauthorizedAccessException("You do not have access to this notification.");
        }

        if (notification.IsRead)
        {
            _logger.LogDebug("Notification {NotificationId} is already read", request.NotificationId);
            return Unit.Value;
        }

        await _notificationRepository.MarkAsReadAsync(request.NotificationId, ct);

        _logger.LogInformation("Notification {NotificationId} marked as read by user {UserId}",
            request.NotificationId, request.UserId);

        return Unit.Value;
    }
}
