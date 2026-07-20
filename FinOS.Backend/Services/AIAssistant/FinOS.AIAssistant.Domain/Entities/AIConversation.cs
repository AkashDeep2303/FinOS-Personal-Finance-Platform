using FinOS.AIAssistant.Domain.Enums;

namespace FinOS.AIAssistant.Domain.Entities;

public class AIConversation
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public QueryType? QueryType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<AIMessage> Messages { get; set; } = new List<AIMessage>();
}
