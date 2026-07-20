using FinOS.Analytics.Domain.Enums;

namespace FinOS.Analytics.Application.DTOs;

public record FinancialScoreDto(
    long Id,
    long UserId,
    DateTime ScoreDate,
    int OverallScore,
    ScoreGrade ScoreGrade,
    int SavingsRateScore,
    int DebtToIncomeScore,
    int EmergencyFundScore,
    int InvestmentScore,
    int GoalProgressScore,
    decimal SavingsRatePct,
    decimal DebtToIncomeRatio,
    decimal EmergencyFundMonths,
    decimal InvestmentToIncomeRatio,
    decimal MonthlyIncome,
    decimal MonthlyExpenses,
    decimal MonthlySavings,
    decimal TotalDebt,
    decimal TotalInvestments,
    string? Recommendations,
    DateTime CreatedAt
);

public record CalculateFinancialScoreDto(
    long UserId,
    decimal MonthlyIncome,
    decimal MonthlyExpenses,
    decimal TotalDebt,
    decimal TotalInvestments,
    decimal EmergencyFundBalance,
    decimal AverageGoalProgressPct
);
