using FinOS.Common.Interfaces;
using FinOS.Notification.Application.Services;
using FinOS.Notification.Domain.Enums;
using FinOS.Notification.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FinOS.Notification.Application.Commands;

/// <summary>
/// Processes all pending scheduled notifications whose scheduled time has arrived.
/// Returns the number of notifications processed.
/// Designed to be called periodically by a background job / hosted service.
/// </summary>
public record ProcessScheduledNotificationsCommand : IRequest<int>;

public class ProcessScheduledNotificationsCommandHandler : IRequestHandler<ProcessScheduledNotificationsCommand, int>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationDeliveryService _deliveryService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessScheduledNotificationsCommandHandler> _logger;

    public ProcessScheduledNotificationsCommandHandler(
        INotificationRepository notificationRepository,
        INotificationDeliveryService deliveryService,
        IUnitOfWork unitOfWork,
        ILogger<ProcessScheduledNotificationsCommandHandler> logger)
    {
        _notificationRepository = notificationRepository;
        _deliveryService = deliveryService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> Handle(ProcessScheduledNotificationsCommand request, CancellationToken ct)
    {
        var scheduled = await _notificationRepository.GetScheduledAsync(ct);
        var now = DateTime.UtcNow;

        var toProcess = scheduled
            .Where(n => n.ScheduledAt <= now && n.DeliveryStatus == DeliveryStatus.Pending)
            .ToList();

        if (toProcess.Count == 0)
        {
            _logger.LogDebug("No scheduled notifications due for delivery");
            return 0;
        }

        _logger.LogInformation("Processing {Count} scheduled notifications", toProcess.Count);

        await _unitOfWork.BeginTransactionAsync();

        var successCount = 0;
        var failCount = 0;

        try
        {
            foreach (var notification in toProcess)
            {
                try
                {
                    await _deliveryService.DeliverAsync(notification, ct);
                    await _notificationRepository.UpdateAsync(notification);
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to deliver scheduled notification {NotificationId} via {Channel}",
                        notification.Id, notification.DeliveryChannel);
                    notification.DeliveryStatus = DeliveryStatus.Failed;
                    await _notificationRepository.UpdateAsync(notification);
                    failCount++;
                }
            }

            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        _logger.LogInformation(
            "Scheduled notification processing complete: {Success} delivered, {Failed} failed",
            successCount, failCount);

        return toProcess.Count;
    }
}
