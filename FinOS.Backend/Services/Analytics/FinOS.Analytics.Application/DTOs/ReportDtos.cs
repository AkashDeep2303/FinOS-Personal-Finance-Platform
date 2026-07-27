namespace FinOS.Analytics.Application.DTOs;
public record FinancialYearReviewDto(string FinancialYear,decimal TotalIncome,decimal TotalExpenses,decimal Savings,decimal SavingsRatePct,decimal OpeningNetWorth,decimal ClosingNetWorth,decimal NetWorthGrowth,decimal NetWorthGrowthPct,string BiggestWin,string BiggestWeakness,string TopSpendingCategory,decimal TopSpendingAmount,IReadOnlyList<MonthlyReportPointDto> Months);
public record MonthlyReportPointDto(int YearMonth,decimal Income,decimal Expenses,decimal Savings);
