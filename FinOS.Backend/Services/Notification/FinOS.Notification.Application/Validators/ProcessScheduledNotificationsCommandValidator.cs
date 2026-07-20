using FinOS.Notification.Application.Commands;
using FluentValidation;

namespace FinOS.Notification.Application.Validators;

/// <summary>
/// ProcessScheduledNotificationsCommand has no input parameters to validate.
/// The validator exists for pipeline consistency and future extensibility.
/// </summary>
public class ProcessScheduledNotificationsCommandValidator : AbstractValidator<ProcessScheduledNotificationsCommand>
{
    public ProcessScheduledNotificationsCommandValidator()
    {
        // No validation rules needed – the command has no properties.
        // Registered for MediatR pipeline behavior consistency.
    }
}
