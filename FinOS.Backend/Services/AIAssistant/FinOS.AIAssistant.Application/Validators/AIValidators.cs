using FinOS.AIAssistant.Application.Commands;
using FluentValidation;

namespace FinOS.AIAssistant.Application.Validators;

public class CreateConversationCommandValidator : AbstractValidator<CreateConversationCommand>
{
    public CreateConversationCommandValidator()
    {
        RuleFor(x => x.Dto.UserId).GreaterThan(0);
        RuleFor(x => x.Dto.Title).NotEmpty().MaximumLength(500);
    }
}

public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.Dto.ConversationId).GreaterThan(0);
        RuleFor(x => x.Dto.UserId).GreaterThan(0);
        RuleFor(x => x.Dto.Content).NotEmpty().MaximumLength(5000);
    }
}

public class SubmitFeedbackCommandValidator : AbstractValidator<SubmitFeedbackCommand>
{
    public SubmitFeedbackCommandValidator()
    {
        RuleFor(x => x.Dto.MessageId).GreaterThan(0);
        RuleFor(x => x.Dto.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Dto.Comment).MaximumLength(2000).When(x => x.Dto.Comment is not null);
    }
}
