using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.AIAssistant.Domain.Entities;

namespace FinOS.AIAssistant.Domain.Interfaces;

public interface IAIConversationRepository : IRepository<AIConversation>
{
    Task<List<AIConversation>> GetByUserIdAsync(long userId, int count, CancellationToken ct = default);
    Task<AIConversation?> GetWithMessagesAsync(long conversationId, CancellationToken ct = default);
    Task<AIConversation> CreateAsync(AIConversation conversation, CancellationToken ct = default);
    new Task UpdateAsync(AIConversation conversation, CancellationToken ct = default);
}
