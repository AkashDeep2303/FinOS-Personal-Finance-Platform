namespace FinOS.Goals.Application.DTOs;

public record GoalProgressDto(
    long GoalId,
    string Name,
    decimal TargetAmount,
    decimal CurrentAmount,
    decimal ProgressPct,
    decimal RemainingAmount,
    decimal MonthlyContribution,
    int RemainingMonths,
    DateTime? ProjectedCompletionDate,
    bool IsOnTrack
);
