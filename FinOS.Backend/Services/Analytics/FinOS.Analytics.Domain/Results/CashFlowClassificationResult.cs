namespace FinOS.Analytics.Domain.Results;

public class CashFlowClassificationResult
{
    public decimal EssentialExpenses { get; set; }
    public decimal LifestyleExpenses { get; set; }
    public decimal EmiPayments { get; set; }
    public decimal Investments { get; set; }
    public decimal OtherExpenses { get; set; }
}

public sealed class MonthlyCashFlowResult : CashFlowClassificationResult
{
    public int YearMonth { get; set; }
    public decimal Income { get; set; }
    public decimal TotalExpenses { get; set; }
}
