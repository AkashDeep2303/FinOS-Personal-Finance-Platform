namespace FinOS.Analytics.Application.DTOs;

public record MonthlyAggregateDto(
    long Id,
    long UserId,
    int YearMonth,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal TotalSavings,
    decimal SavingsRate,
    string? TopExpenseCategory,
    decimal TopExpenseAmount,
    int TransactionCount,
    string? CategoryBreakdown,
    DateTime CreatedAt
);

public record GenerateMonthlyAggregatesDto(
    long UserId,
    int YearMonth
);
