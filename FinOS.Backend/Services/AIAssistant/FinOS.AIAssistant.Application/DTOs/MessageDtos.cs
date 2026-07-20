using FinOS.AIAssistant.Domain.Enums;

namespace FinOS.AIAssistant.Application.DTOs;

public record MessageDto(
    long Id,
    long ConversationId,
    MessageRole Role,
    string Content,
    QueryType QueryType,
    string? ReferencedEntityIds,
    int TokenCount,
    long ResponseTimeMs,
    DateTime CreatedAt
);

public record SendMessageDto(
    long ConversationId,
    long UserId,
    string Content,
    QueryType QueryType
);

public record MessageResponseDto(
    MessageDto UserMessage,
    MessageDto AssistantMessage
);
