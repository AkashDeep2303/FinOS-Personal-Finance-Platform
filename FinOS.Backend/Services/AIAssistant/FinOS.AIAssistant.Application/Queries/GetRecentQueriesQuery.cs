using FinOS.AIAssistant.Application.DTOs;
using FinOS.AIAssistant.Domain.Enums;
using FinOS.AIAssistant.Domain.Interfaces;
using MediatR;

namespace FinOS.AIAssistant.Application.Queries;

public record GetRecentQueriesQuery(long UserId, int Count = 10) : IRequest<List<MessageDto>>;

public class GetRecentQueriesQueryHandler : IRequestHandler<GetRecentQueriesQuery, List<MessageDto>>
{
    private readonly IAIMessageRepository _messageRepository;

    public GetRecentQueriesQueryHandler(IAIMessageRepository messageRepository)
    {
        _messageRepository = messageRepository;
    }

    public async Task<List<MessageDto>> Handle(GetRecentQueriesQuery request, CancellationToken ct)
    {
        var messages = await _messageRepository.GetRecentByUserAsync(request.UserId, request.Count, ct);

        return messages.Select(m => new MessageDto(
            m.Id, m.ConversationId, m.Role, m.Content, m.QueryType,
            m.ReferencedEntityIds, m.TokenCount, m.ResponseTimeMs, m.CreatedAt
        )).OrderByDescending(m => m.CreatedAt).ToList();
    }
}
