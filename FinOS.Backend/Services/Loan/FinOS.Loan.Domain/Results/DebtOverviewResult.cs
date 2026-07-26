namespace FinOS.Loan.Domain.Results;

public class DebtOverviewResult
{
    public decimal TotalOutstandingDebt { get; set; }
    public decimal TotalMonthlyEMI { get; set; }
    public int ActiveLoanCount { get; set; }
    public decimal MonthlyIncome { get; set; }
    public decimal DebtToIncomeRatioPct { get; set; }
    public string RiskCategory { get; set; } = "Unknown";
    public decimal MonthlySurplusAfterEMI { get; set; }
    public decimal WeightedInterestRate { get; set; }
    public DateTime? DebtFreeDate { get; set; }
}
