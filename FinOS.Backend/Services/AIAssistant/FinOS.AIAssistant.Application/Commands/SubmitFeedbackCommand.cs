using FinOS.AIAssistant.Application.DTOs;
using FinOS.AIAssistant.Domain.Interfaces;
using FinOS.Common.Exceptions;
using MediatR;

namespace FinOS.AIAssistant.Application.Commands;

public record SubmitFeedbackCommand(FeedbackDto Dto) : IRequest<Unit>;

public class SubmitFeedbackCommandHandler : IRequestHandler<SubmitFeedbackCommand, Unit>
{
    private readonly IAIMessageRepository _messageRepository;

    public SubmitFeedbackCommandHandler(IAIMessageRepository messageRepository)
    {
        _messageRepository = messageRepository;
    }

    public async Task<Unit> Handle(SubmitFeedbackCommand request, CancellationToken ct)
    {
        var dto = request.Dto;
        var message = await _messageRepository.GetByIdAsync(dto.MessageId, ct)
            ?? throw new NotFoundException("Message", dto.MessageId);

        message.FeedbackRating = dto.Rating;
        message.FeedbackComment = dto.Comment;

        await _messageRepository.UpdateAsync(message, ct);

        return Unit.Value;
    }
}
