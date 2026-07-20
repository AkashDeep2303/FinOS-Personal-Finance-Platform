namespace FinOS.Loan.Domain.Results;

/// <summary>
/// Result DTO returned by the Loan.sp_SimulatePrepayment stored procedure.
/// </summary>
public class PrepaymentSimulationResult
{
    public decimal PrepaymentAmount { get; set; }
    public decimal PenaltyEstimate { get; set; }
    public string Strategy { get; set; } = string.Empty;
    public decimal OriginalOutstanding { get; set; }
    public decimal NewOutstanding { get; set; }
    public decimal OriginalEMI { get; set; }
    public decimal NewEMI { get; set; }
    public int OriginalTenureMonths { get; set; }
    public int NewTenureMonths { get; set; }
    public int TenureSaved { get; set; }
    public decimal OriginalTotalInterest { get; set; }
    public decimal NewTotalInterest { get; set; }
    public decimal InterestSaved { get; set; }
    public decimal TotalCashRequired { get; set; }
}
