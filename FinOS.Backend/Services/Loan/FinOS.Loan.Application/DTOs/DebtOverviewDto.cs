namespace FinOS.Loan.Application.DTOs;

public record DebtOverviewDto(
    decimal TotalOutstandingDebt,
    decimal TotalMonthlyEmi,
    int ActiveLoanCount,
    decimal MonthlyIncome,
    decimal DebtToIncomeRatioPct,
    string RiskCategory,
    decimal MonthlySurplusAfterEmi,
    decimal WeightedInterestRate,
    DateTime? DebtFreeDate);
