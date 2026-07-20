using FinOS.Common.Exceptions;
using FinOS.Common.Interfaces;
using FinOS.Notification.Application.DTOs;
using NotificationPreferenceEntity = FinOS.Notification.Domain.Entities.NotificationPreference;
using FinOS.Notification.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FinOS.Notification.Application.Commands;

/// <summary>
/// Creates or updates a user's notification preference for a specific notification type.
/// Only non-null fields in the DTO are applied (partial update semantics).
/// </summary>
public record UpdateNotificationPreferenceCommand(UpdateNotificationPreferenceDto Dto) : IRequest<NotificationPreferenceDto>;

public class UpdateNotificationPreferenceCommandHandler : IRequestHandler<UpdateNotificationPreferenceCommand, NotificationPreferenceDto>
{
    private readonly INotificationPreferenceRepository _preferenceRepository;
    private readonly INotificationTypeRepository _typeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateNotificationPreferenceCommandHandler> _logger;

    public UpdateNotificationPreferenceCommandHandler(
        INotificationPreferenceRepository preferenceRepository,
        INotificationTypeRepository typeRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateNotificationPreferenceCommandHandler> logger)
    {
        _preferenceRepository = preferenceRepository;
        _typeRepository = typeRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<NotificationPreferenceDto> Handle(UpdateNotificationPreferenceCommand request, CancellationToken ct)
    {
        var dto = request.Dto;

        // Validate notification type exists
        var notificationType = await _typeRepository.GetByIdAsync(dto.NotificationTypeId, ct)
            ?? throw new NotFoundException("NotificationType", dto.NotificationTypeId);

        var preference = await _preferenceRepository.GetByUserAndTypeAsync(dto.UserId, dto.NotificationTypeId, ct);

        // Dapper repos persist immediately — wrap in transaction for atomicity
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            if (preference is null)
            {
                // Create default preference
                preference = new NotificationPreferenceEntity
                {
                    UserId = dto.UserId,
                    NotificationTypeId = dto.NotificationTypeId,
                    EmailEnabled = true,
                    PushEnabled = true,
                    SmsEnabled = false,
                    InAppEnabled = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _preferenceRepository.AddAsync(preference, ct);
                _logger.LogInformation(
                    "Created new notification preference for user {UserId}, type {TypeId}",
                    dto.UserId, dto.NotificationTypeId);
            }

            // Apply partial updates – only overwrite supplied values
            if (dto.EmailEnabled.HasValue) preference.EmailEnabled = dto.EmailEnabled.Value;
            if (dto.PushEnabled.HasValue) preference.PushEnabled = dto.PushEnabled.Value;
            if (dto.SmsEnabled.HasValue) preference.SmsEnabled = dto.SmsEnabled.Value;
            if (dto.InAppEnabled.HasValue) preference.InAppEnabled = dto.InAppEnabled.Value;

            if (dto.QuietHoursStart is not null)
                preference.QuietHoursStart = TimeSpan.Parse(dto.QuietHoursStart);

            if (dto.QuietHoursEnd is not null)
                preference.QuietHoursEnd = TimeSpan.Parse(dto.QuietHoursEnd);

            preference.UpdatedAt = DateTime.UtcNow;
            await _preferenceRepository.UpdateAsync(preference);

            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        _logger.LogInformation(
            "Updated notification preference {PreferenceId} for user {UserId}",
            preference.Id, dto.UserId);

        return new NotificationPreferenceDto(
            preference.Id,
            preference.UserId,
            preference.NotificationTypeId,
            notificationType.Name,
            preference.EmailEnabled,
            preference.PushEnabled,
            preference.SmsEnabled,
            preference.InAppEnabled,
            preference.QuietHoursStart?.ToString(@"hh\:mm"),
            preference.QuietHoursEnd?.ToString(@"hh\:mm")
        );
    }
}
