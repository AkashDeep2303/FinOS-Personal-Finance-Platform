using FinOS.Notification.Application.DTOs;
using FinOS.Notification.Domain.Interfaces;
using MediatR;

namespace FinOS.Notification.Application.Queries;

/// <summary>
/// Returns the count of unread notifications for a user.
/// </summary>
public record GetUnreadCountQuery(long UserId) : IRequest<UnreadCountDto>;

public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, UnreadCountDto>
{
    private readonly INotificationRepository _notificationRepository;

    public GetUnreadCountQueryHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<UnreadCountDto> Handle(GetUnreadCountQuery request, CancellationToken ct)
    {
        var count = await _notificationRepository.GetUnreadCountAsync(request.UserId, ct);
        return new UnreadCountDto(request.UserId, count);
    }
}
