using FinOS.Notification.Application.Commands;
using FluentValidation;

namespace FinOS.Notification.Application.Validators;

public class UpdateNotificationPreferenceCommandValidator : AbstractValidator<UpdateNotificationPreferenceCommand>
{
    public UpdateNotificationPreferenceCommandValidator()
    {
        RuleFor(x => x.Dto.UserId)
            .GreaterThan(0).WithMessage("UserId must be a positive integer.");

        RuleFor(x => x.Dto.NotificationTypeId)
            .GreaterThan(0).WithMessage("NotificationTypeId must be a positive integer.");

        RuleFor(x => x.Dto.QuietHoursStart)
            .Must(BeAValidTimeOrNull).WithMessage("QuietHoursStart must be a valid time format (e.g., '22:00').")
            .When(x => x.Dto.QuietHoursStart is not null);

        RuleFor(x => x.Dto.QuietHoursEnd)
            .Must(BeAValidTimeOrNull).WithMessage("QuietHoursEnd must be a valid time format (e.g., '07:30').")
            .When(x => x.Dto.QuietHoursEnd is not null);

        RuleFor(x => x.Dto)
            .Must(HaveAtLeastOneChannelEnabled)
            .WithMessage("At least one delivery channel must remain enabled.")
            .When(x => x.Dto.EmailEnabled.HasValue || x.Dto.PushEnabled.HasValue
                     || x.Dto.SmsEnabled.HasValue || x.Dto.InAppEnabled.HasValue);
    }

    private static bool BeAValidTimeOrNull(string? time)
    {
        if (time is null) return true;
        return TimeSpan.TryParse(time, out _);
    }

    /// <summary>
    /// If the DTO is trying to disable channels, ensure at least one remains enabled.
    /// This only validates the *intent* of the DTO; the actual persisted state may differ
    /// because only supplied (non-null) values are applied.
    /// </summary>
    private static bool HaveAtLeastOneChannelEnabled(Application.DTOs.UpdateNotificationPreferenceDto dto)
    {
        // If any channel is explicitly set to false, ensure not all are being disabled
        var email = dto.EmailEnabled ?? true;
        var push = dto.PushEnabled ?? true;
        var sms = dto.SmsEnabled ?? false; // SMS defaults to disabled
        var inApp = dto.InAppEnabled ?? true;

        return email || push || sms || inApp;
    }
}
