namespace FinOS.AIAssistant.Application.DTOs;

public record FeedbackDto(
    long MessageId,
    int Rating,
    string? Comment
);
