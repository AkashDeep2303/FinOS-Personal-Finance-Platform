using FinOS.Notification.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FinOS.Notification.Application.Commands;

/// <summary>
/// Marks all unread notifications for a user as read.
/// Dapper repos persist immediately — no SaveChangesAsync needed.
/// </summary>
public record MarkAllAsReadCommand(long UserId) : IRequest<Unit>;

public class MarkAllAsReadCommandHandler : IRequestHandler<MarkAllAsReadCommand, Unit>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<MarkAllAsReadCommandHandler> _logger;

    public MarkAllAsReadCommandHandler(
        INotificationRepository notificationRepository,
        ILogger<MarkAllAsReadCommandHandler> logger)
    {
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(MarkAllAsReadCommand request, CancellationToken ct)
    {
        await _notificationRepository.MarkAllAsReadAsync(request.UserId, ct);

        _logger.LogInformation("All notifications marked as read for user {UserId}", request.UserId);

        return Unit.Value;
    }
}
