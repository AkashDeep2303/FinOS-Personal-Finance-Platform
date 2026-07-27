namespace FinOS.Goals.Application.DTOs;

public record GoalFundingAnalysisDto(
    decimal AvailableMonthlySurplus,
    decimal TotalRequiredMonthlyContribution,
    decimal FundingDeficit,
    bool HasConflict,
    IReadOnlyList<GoalFundingItemDto> Goals);

public record GoalFundingItemDto(
    long GoalId,
    string Name,
    string Category,
    string Priority,
    decimal TargetAmount,
    decimal CurrentAmount,
    decimal RemainingAmount,
    DateTime? TargetDate,
    decimal RequiredMonthlyContribution,
    decimal ActualMonthlyContribution,
    DateTime? ProjectedCompletionDate,
    int? ScheduleVarianceMonths,
    string Status);
