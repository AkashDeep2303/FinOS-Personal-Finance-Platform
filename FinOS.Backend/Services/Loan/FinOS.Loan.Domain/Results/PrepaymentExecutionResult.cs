namespace FinOS.Loan.Domain.Results;

/// <summary>
/// Result DTO returned by the Loan.sp_ExecutePrepayment stored procedure.
/// </summary>
public class PrepaymentExecutionResult
{
    public decimal PrepaymentAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public string PrepaymentType { get; set; } = string.Empty;
    public string Strategy { get; set; } = string.Empty;
    public decimal PreviousOutstanding { get; set; }
    public decimal NewOutstanding { get; set; }
    public decimal PreviousEMI { get; set; }
    public decimal NewEMI { get; set; }
    public int PreviousTenureMonths { get; set; }
    public int NewTenureMonths { get; set; }
    public decimal InterestSaved { get; set; }
    public string LoanStatus { get; set; } = string.Empty;
}
