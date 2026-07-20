using FinOS.Notification.Application.DTOs;
using FinOS.Notification.Domain.Interfaces;
using MediatR;

namespace FinOS.Notification.Application.Queries;

/// <summary>
/// Returns all enabled notification types.
/// </summary>
public record GetNotificationTypesQuery : IRequest<List<NotificationTypeDto>>;

public class GetNotificationTypesQueryHandler : IRequestHandler<GetNotificationTypesQuery, List<NotificationTypeDto>>
{
    private readonly INotificationTypeRepository _typeRepository;

    public GetNotificationTypesQueryHandler(INotificationTypeRepository typeRepository)
    {
        _typeRepository = typeRepository;
    }

    public async Task<List<NotificationTypeDto>> Handle(GetNotificationTypesQuery request, CancellationToken ct)
    {
        var types = await _typeRepository.GetEnabledAsync(ct);

        return types.Select(t => new NotificationTypeDto(
            t.Id, t.Name, t.Description, t.Category, t.IsEnabled
        )).ToList();
    }
}
