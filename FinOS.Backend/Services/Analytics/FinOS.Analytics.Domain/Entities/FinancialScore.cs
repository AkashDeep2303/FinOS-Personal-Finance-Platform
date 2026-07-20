using FinOS.Analytics.Domain.Enums;

namespace FinOS.Analytics.Domain.Entities;

public class FinancialScore
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public DateTime ScoreDate { get; set; }
    public int OverallScore { get; set; }
    public ScoreGrade ScoreGrade { get; set; }
    public int SavingsRateScore { get; set; }
    public int DebtToIncomeScore { get; set; }
    public int EmergencyFundScore { get; set; }
    public int InvestmentScore { get; set; }
    public int GoalProgressScore { get; set; }
    public decimal SavingsRatePct { get; set; }
    public decimal DebtToIncomeRatio { get; set; }
    public decimal EmergencyFundMonths { get; set; }
    public decimal InvestmentToIncomeRatio { get; set; }
    public decimal MonthlyIncome { get; set; }
    public decimal MonthlyExpenses { get; set; }
    public decimal MonthlySavings { get; set; }
    public decimal TotalDebt { get; set; }
    public decimal TotalInvestments { get; set; }
    public string? Recommendations { get; set; }
    public DateTime CreatedAt { get; set; }
}
