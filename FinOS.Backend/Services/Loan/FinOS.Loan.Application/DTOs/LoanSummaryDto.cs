namespace FinOS.Loan.Application.DTOs;

public class LoanSummaryDto
{
    public long LoanId { get; set; }
    public string LenderName { get; set; } = string.Empty;
    public decimal PrincipalAmount { get; set; }
    public decimal OutstandingPrincipal { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalInterestPaid { get; set; }
    public decimal TotalPrepaid { get; set; }
    public decimal InterestSaved { get; set; }
    public decimal PaidPercentage { get; set; }
    public int RemainingTenureMonths { get; set; }
    public DateTime? NextEMIDate { get; set; }
}
