using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.AIAssistant.Domain.Entities;
using FinOS.AIAssistant.Domain.Enums;

namespace FinOS.AIAssistant.Domain.Interfaces;

public interface IAIMessageRepository : IRepository<AIMessage>
{
    Task<List<AIMessage>> GetByConversationIdAsync(long conversationId, CancellationToken ct = default);
    Task<List<AIMessage>> GetRecentByUserAsync(long userId, int count, CancellationToken ct = default);
    Task<AIMessage> CreateAsync(AIMessage message, CancellationToken ct = default);
    Task UpdateAsync(AIMessage message, CancellationToken ct = default);
}
