using FinOS.Common.Models;
using FinOS.Notification.Application.DTOs;
using FinOS.Notification.Domain.Interfaces;
using MediatR;

namespace FinOS.Notification.Application.Queries;

/// <summary>
/// Paginated, filtered listing of notifications for a specific user.
/// </summary>
public record GetNotificationsByUserQuery(
    long UserId,
    bool? IsRead,
    int? NotificationTypeId,
    PagedQuery Paging) : IRequest<PagedResult<NotificationDto>>;

public class GetNotificationsByUserQueryHandler : IRequestHandler<GetNotificationsByUserQuery, PagedResult<NotificationDto>>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationTypeRepository _typeRepository;

    public GetNotificationsByUserQueryHandler(
        INotificationRepository notificationRepository,
        INotificationTypeRepository typeRepository)
    {
        _notificationRepository = notificationRepository;
        _typeRepository = typeRepository;
    }

    public async Task<PagedResult<NotificationDto>> Handle(GetNotificationsByUserQuery request, CancellationToken ct)
    {
        var result = await _notificationRepository.GetPagedByUserAsync(
            request.UserId, request.IsRead, request.NotificationTypeId, request.Paging, ct);

        // Build a lookup for notification type names
        var typeIds = result.Items
            .Select(n => n.NotificationTypeId)
            .Distinct()
            .ToList();

        var typeLookup = new Dictionary<int, string>();
        foreach (var typeId in typeIds)
        {
            var type = await _typeRepository.GetByIdAsync(typeId, ct);
            if (type is not null)
                typeLookup[typeId] = type.Name;
        }

        return new PagedResult<NotificationDto>
        {
            Items = result.Items.Select(n => new NotificationDto(
                n.Id, n.UserId, n.NotificationTypeId,
                typeLookup.GetValueOrDefault(n.NotificationTypeId, "Unknown"),
                n.Title, n.Message, n.DeepLink, n.EntityType, n.EntityId,
                n.IsRead, n.ReadAt, n.IsActionTaken, n.ActionTakenAt,
                n.DeliveryChannel, n.DeliveryStatus,
                n.ScheduledAt, n.SentAt, n.ExpiresAt, n.CreatedAt
            )).ToList(),
            TotalCount = result.TotalCount,
            Page = result.PageNumber,
            PageSize = result.PageSize
        };
    }
}
