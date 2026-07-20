using FinOS.AIAssistant.Application.DTOs;
using FinOS.AIAssistant.Domain.Interfaces;
using MediatR;

namespace FinOS.AIAssistant.Application.Queries;

public record GetConversationsByUserQuery(long UserId, int Count = 20) : IRequest<List<ConversationDto>>;

public class GetConversationsByUserQueryHandler : IRequestHandler<GetConversationsByUserQuery, List<ConversationDto>>
{
    private readonly IAIConversationRepository _conversationRepository;

    public GetConversationsByUserQueryHandler(IAIConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository;
    }

    public async Task<List<ConversationDto>> Handle(GetConversationsByUserQuery request, CancellationToken ct)
    {
        var conversations = await _conversationRepository.GetByUserIdAsync(request.UserId, request.Count, ct);

        return conversations.Select(c => new ConversationDto(
            c.Id, c.UserId, c.Title, c.CreatedAt, c.UpdatedAt,
            c.Messages?.Count ?? 0
        )).OrderByDescending(c => c.UpdatedAt).ToList();
    }
}
