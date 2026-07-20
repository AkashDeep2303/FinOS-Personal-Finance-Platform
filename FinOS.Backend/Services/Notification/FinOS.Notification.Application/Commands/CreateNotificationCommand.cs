using FinOS.Common.Interfaces;
using FinOS.Notification.Application.DTOs;
using FinOS.Notification.Application.Services;
using NotificationEntity = FinOS.Notification.Domain.Entities.Notification;
using FinOS.Notification.Domain.Enums;
using FinOS.Notification.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FinOS.Notification.Application.Commands;

/// <summary>
/// Creates a new notification and, if not scheduled for the future, delivers it immediately.
/// </summary>
public record CreateNotificationCommand(CreateNotificationDto Dto) : IRequest<NotificationDto>;

public class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand, NotificationDto>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationTypeRepository _typeRepository;
    private readonly INotificationDeliveryService _deliveryService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateNotificationCommandHandler> _logger;

    public CreateNotificationCommandHandler(
        INotificationRepository notificationRepository,
        INotificationTypeRepository typeRepository,
        INotificationDeliveryService deliveryService,
        IUnitOfWork unitOfWork,
        ILogger<CreateNotificationCommandHandler> logger)
    {
        _notificationRepository = notificationRepository;
        _typeRepository = typeRepository;
        _deliveryService = deliveryService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<NotificationDto> Handle(CreateNotificationCommand request, CancellationToken ct)
    {
        var dto = request.Dto;

        // Validate notification type exists
        var notificationType = await _typeRepository.GetByIdAsync(dto.NotificationTypeId, ct)
            ?? throw new FinOS.Common.Exceptions.NotFoundException("NotificationType", dto.NotificationTypeId);

        var notification = new NotificationEntity
        {
            UserId = dto.UserId,
            NotificationTypeId = dto.NotificationTypeId,
            Title = dto.Title,
            Message = dto.Message,
            DeepLink = dto.DeepLink,
            EntityType = dto.EntityType,
            EntityId = dto.EntityId,
            DeliveryChannel = dto.DeliveryChannel,
            DeliveryStatus = DeliveryStatus.Pending,
            ScheduledAt = dto.ScheduledAt,
            ExpiresAt = dto.ExpiresAt,
            CreatedAt = DateTime.UtcNow
        };

        // Dapper repos persist immediately — wrap in transaction for atomicity
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            await _notificationRepository.AddAsync(notification, ct);

            _logger.LogInformation(
                "Notification {NotificationId} created for user {UserId} with channel {Channel}",
                notification.Id, notification.UserId, notification.DeliveryChannel);

            // Deliver immediately if not scheduled for the future
            if (notification.ScheduledAt == null || notification.ScheduledAt <= DateTime.UtcNow)
            {
                try
                {
                    await _deliveryService.DeliverAsync(notification, ct);
                    await _notificationRepository.UpdateAsync(notification);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to deliver notification {NotificationId} immediately after creation",
                        notification.Id);
                    notification.DeliveryStatus = DeliveryStatus.Failed;
                    await _notificationRepository.UpdateAsync(notification);
                }
            }

            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        return MapToDto(notification, notificationType.Name);
    }

    private static NotificationDto MapToDto(NotificationEntity n, string typeName) => new(
        n.Id, n.UserId, n.NotificationTypeId, typeName,
        n.Title, n.Message, n.DeepLink, n.EntityType, n.EntityId,
        n.IsRead, n.ReadAt, n.IsActionTaken, n.ActionTakenAt,
        n.DeliveryChannel, n.DeliveryStatus,
        n.ScheduledAt, n.SentAt, n.ExpiresAt, n.CreatedAt
    );
}
