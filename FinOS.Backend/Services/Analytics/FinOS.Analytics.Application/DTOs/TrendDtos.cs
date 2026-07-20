namespace FinOS.Analytics.Application.DTOs;

public record SpendingTrendDto(
    int YearMonth,
    string Category,
    decimal Amount
);

public record IncomeVsExpenseDto(
    int YearMonth,
    decimal Income,
    decimal Expense,
    decimal Savings
);

public record CategoryBreakdownDto(
    string Category,
    decimal Amount,
    decimal Percentage
);
