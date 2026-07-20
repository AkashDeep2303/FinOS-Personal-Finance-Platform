using FinOS.AIAssistant.Application.DTOs;
using FinOS.AIAssistant.Domain.Entities;
using FinOS.AIAssistant.Domain.Interfaces;
using MediatR;

namespace FinOS.AIAssistant.Application.Commands;

public record CreateConversationCommand(CreateConversationDto Dto) : IRequest<ConversationDto>;

public class CreateConversationCommandHandler : IRequestHandler<CreateConversationCommand, ConversationDto>
{
    private readonly IAIConversationRepository _conversationRepository;

    public CreateConversationCommandHandler(IAIConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository;
    }

    public async Task<ConversationDto> Handle(CreateConversationCommand request, CancellationToken ct)
    {
        var conversation = new AIConversation
        {
            UserId = request.Dto.UserId,
            Title = request.Dto.Title,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        conversation = await _conversationRepository.CreateAsync(conversation, ct);

        return new ConversationDto(
            conversation.Id, conversation.UserId, conversation.Title,
            conversation.CreatedAt, conversation.UpdatedAt, 0
        );
    }
}
