namespace FinOS.AIAssistant.Application.DTOs;

public record ConversationDto(
    long Id,
    long UserId,
    string Title,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int MessageCount
);

public record CreateConversationDto(
    long UserId,
    string Title
);
