namespace FinOS.Loan.Application.DTOs;

public class EMIScheduleDto
{
    public long Id { get; set; }
    public long LoanId { get; set; }
    public int EMINumber { get; set; }
    public DateTime EMIDate { get; set; }
    public decimal EMIAmount { get; set; }
    public decimal PrincipalComponent { get; set; }
    public decimal InterestComponent { get; set; }
    public decimal OutstandingBefore { get; set; }
    public decimal OutstandingAfter { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidDate { get; set; }
    public decimal? PaidAmount { get; set; }
    public decimal LateFee { get; set; }
}

public class RecordEMIPaymentRequest
{
    public long LoanId { get; set; }
    public int EMINumber { get; set; }
    public DateTime PaidDate { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal LateFee { get; set; }
}
