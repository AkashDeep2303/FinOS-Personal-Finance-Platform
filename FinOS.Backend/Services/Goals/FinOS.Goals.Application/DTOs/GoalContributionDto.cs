namespace FinOS.Goals.Application.DTOs;

public record GoalContributionDto(
    long Id,
    long GoalId,
    decimal Amount,
    DateTime ContributionDate,
    string Source,
    long? SourceAccountId,
    string? Notes,
    DateTime CreatedAt
);

public record AddGoalContributionDto(
    long GoalId,
    decimal Amount,
    DateTime ContributionDate,
    string Source = "Manual",
    long? SourceAccountId = null,
    string? Notes = null
);
