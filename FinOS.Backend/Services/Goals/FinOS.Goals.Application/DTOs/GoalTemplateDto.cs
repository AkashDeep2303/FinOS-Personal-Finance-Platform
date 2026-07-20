namespace FinOS.Goals.Application.DTOs;

public record GoalTemplateDto(
    long Id,
    string Name,
    string? Description,
    string Category,
    decimal SuggestedAmount,
    int SuggestedMonths,
    string? Icon,
    string? Color,
    int SortOrder
);
