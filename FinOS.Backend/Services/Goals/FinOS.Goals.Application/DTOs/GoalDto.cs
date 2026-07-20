using FinOS.Goals.Domain.Enums;

namespace FinOS.Goals.Application.DTOs;

public record GoalDto(
    long Id,
    long UserId,
    long? GoalTemplateId,
    string Name,
    string? Description,
    string Category,
    decimal TargetAmount,
    decimal CurrentAmount,
    decimal MonthlyContribution,
    DateTime StartDate,
    DateTime? TargetDate,
    DateTime? CompletedDate,
    GoalPriority Priority,
    GoalStatus Status,
    string? LinkedAccountIds,
    string? Icon,
    string? Color,
    bool IsAutoContribute,
    DateTime? ProjectedDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateGoalDto(
    long UserId,
    long? GoalTemplateId,
    string Name,
    string? Description,
    string Category,
    decimal TargetAmount,
    decimal MonthlyContribution,
    DateTime StartDate,
    DateTime? TargetDate,
    GoalPriority Priority,
    string? LinkedAccountIds,
    string? Icon,
    string? Color,
    bool IsAutoContribute
);

public record UpdateGoalDto(
    long Id,
    string? Name,
    string? Description,
    string? Category,
    decimal? TargetAmount,
    decimal? MonthlyContribution,
    DateTime? TargetDate,
    GoalPriority? Priority,
    string? LinkedAccountIds,
    string? Icon,
    string? Color,
    bool? IsAutoContribute
);
