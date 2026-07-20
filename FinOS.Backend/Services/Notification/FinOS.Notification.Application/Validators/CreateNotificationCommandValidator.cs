using FinOS.Notification.Application.Commands;
using FinOS.Notification.Domain.Enums;
using FluentValidation;

namespace FinOS.Notification.Application.Validators;

public class CreateNotificationCommandValidator : AbstractValidator<CreateNotificationCommand>
{
    private static readonly DeliveryChannel[] ValidChannels =
        Enum.GetValues<DeliveryChannel>();

    public CreateNotificationCommandValidator()
    {
        RuleFor(x => x.Dto.UserId)
            .GreaterThan(0).WithMessage("UserId must be a positive integer.");

        RuleFor(x => x.Dto.NotificationTypeId)
            .GreaterThan(0).WithMessage("NotificationTypeId must be a positive integer.");

        RuleFor(x => x.Dto.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(500).WithMessage("Title must not exceed 500 characters.");

        RuleFor(x => x.Dto.Message)
            .NotEmpty().WithMessage("Message is required.")
            .MaximumLength(5000).WithMessage("Message must not exceed 5000 characters.");

        RuleFor(x => x.Dto.DeepLink)
            .MaximumLength(1000).WithMessage("DeepLink must not exceed 1000 characters.")
            .Must(BeAValidUriOrNull).WithMessage("DeepLink must be a valid URI.");

        RuleFor(x => x.Dto.EntityType)
            .MaximumLength(100).WithMessage("EntityType must not exceed 100 characters.");

        RuleFor(x => x.Dto.EntityId)
            .MaximumLength(100).WithMessage("EntityId must not exceed 100 characters.");

        RuleFor(x => x.Dto.DeliveryChannel)
            .IsInEnum().WithMessage("DeliveryChannel must be a valid value (InApp, Email, Push, SMS).");

        RuleFor(x => x.Dto.ScheduledAt)
            .Must(BeInTheFutureOrNull).WithMessage("ScheduledAt must be a future date if provided.");

        RuleFor(x => x.Dto.ExpiresAt)
            .Must(BeInTheFutureOrNull).WithMessage("ExpiresAt must be a future date if provided.")
            .GreaterThan(x => x.Dto.ScheduledAt)
            .When(x => x.Dto.ScheduledAt.HasValue && x.Dto.ExpiresAt.HasValue)
            .WithMessage("ExpiresAt must be after ScheduledAt.");
    }

    private static bool BeAValidUriOrNull(string? uri)
    {
        if (uri is null) return true;
        return Uri.TryCreate(uri, UriKind.Absolute, out _);
    }

    private static bool BeInTheFutureOrNull(DateTime? date)
    {
        if (date is null) return true;
        return date.Value > DateTime.UtcNow;
    }
}
