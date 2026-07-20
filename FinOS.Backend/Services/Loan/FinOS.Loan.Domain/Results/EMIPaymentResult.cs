namespace FinOS.Loan.Domain.Results;

/// <summary>
/// Result DTO returned by the Loan.sp_RecordEMIPayment stored procedure.
/// </summary>
public class EMIPaymentResult
{
    public int EMINumber { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PrincipalPaid { get; set; }
    public decimal InterestPaid { get; set; }
    public decimal LateFee { get; set; }
    public decimal RemainingOutstanding { get; set; }
    public int RemainingEMIs { get; set; }
}
