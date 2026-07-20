using FinOS.Notification.Application.Commands;
using FluentValidation;

namespace FinOS.Notification.Application.Validators;

public class MarkAllAsReadCommandValidator : AbstractValidator<MarkAllAsReadCommand>
{
    public MarkAllAsReadCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("UserId must be a positive integer.");
    }
}
