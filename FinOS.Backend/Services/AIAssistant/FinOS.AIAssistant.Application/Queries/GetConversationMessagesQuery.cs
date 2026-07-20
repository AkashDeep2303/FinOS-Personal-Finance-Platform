using FinOS.AIAssistant.Application.DTOs;
using FinOS.AIAssistant.Domain.Interfaces;
using FinOS.Common.Exceptions;
using MediatR;

namespace FinOS.AIAssistant.Application.Queries;

public record GetConversationMessagesQuery(long ConversationId, long UserId) : IRequest<List<MessageDto>>;

public class GetConversationMessagesQueryHandler : IRequestHandler<GetConversationMessagesQuery, List<MessageDto>>
{
    private readonly IAIConversationRepository _conversationRepository;
    private readonly IAIMessageRepository _messageRepository;

    public GetConversationMessagesQueryHandler(IAIConversationRepository conversationRepository, IAIMessageRepository messageRepository)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
    }

    public async Task<List<MessageDto>> Handle(GetConversationMessagesQuery request, CancellationToken ct)
    {
        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, ct)
            ?? throw new NotFoundException("Conversation", request.ConversationId);

        if (conversation.UserId != request.UserId)
            throw new UnauthorizedAccessException();

        var messages = await _messageRepository.GetByConversationIdAsync(request.ConversationId, ct);

        return messages.Select(m => new MessageDto(
            m.Id, m.ConversationId, m.Role, m.Content, m.QueryType,
            m.ReferencedEntityIds, m.TokenCount, m.ResponseTimeMs, m.CreatedAt
        )).OrderBy(m => m.CreatedAt).ToList();
    }
}
