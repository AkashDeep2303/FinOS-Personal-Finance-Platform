using FinOS.Notification.Application.DTOs;
using FinOS.Notification.Domain.Interfaces;
using MediatR;

namespace FinOS.Notification.Application.Queries;

/// <summary>
/// Returns all notification preferences for a user, including type names.
/// </summary>
public record GetNotificationPreferencesQuery(long UserId) : IRequest<List<NotificationPreferenceDto>>;

public class GetNotificationPreferencesQueryHandler : IRequestHandler<GetNotificationPreferencesQuery, List<NotificationPreferenceDto>>
{
    private readonly INotificationPreferenceRepository _preferenceRepository;

    public GetNotificationPreferencesQueryHandler(INotificationPreferenceRepository preferenceRepository)
    {
        _preferenceRepository = preferenceRepository;
    }

    public async Task<List<NotificationPreferenceDto>> Handle(GetNotificationPreferencesQuery request, CancellationToken ct)
    {
        var preferences = await _preferenceRepository.GetByUserAsync(request.UserId, ct);

        return preferences.Select(p => new NotificationPreferenceDto(
            p.Id,
            p.UserId,
            p.NotificationTypeId,
            p.NotificationType?.Name ?? "Unknown",
            p.EmailEnabled,
            p.PushEnabled,
            p.SmsEnabled,
            p.InAppEnabled,
            p.QuietHoursStart?.ToString(@"hh\:mm"),
            p.QuietHoursEnd?.ToString(@"hh\:mm")
        )).ToList();
    }
}
