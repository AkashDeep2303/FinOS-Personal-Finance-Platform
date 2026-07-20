using FinOS.AIAssistant.Domain.Enums;

namespace FinOS.AIAssistant.Domain.Entities;

public class AIMessage
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public QueryType QueryType { get; set; } = QueryType.General;
    public string? ReferencedEntityIds { get; set; }
    public int TokenCount { get; set; }
    public long ResponseTimeMs { get; set; }
    public int? FeedbackRating { get; set; }
    public string? FeedbackComment { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public AIConversation? Conversation { get; set; }
}
