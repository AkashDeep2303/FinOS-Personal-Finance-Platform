using FinOS.Loan.Domain.Enums;

namespace FinOS.Loan.Application.DTOs;

public class LoanDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long LoanTypeId { get; set; }
    public string LoanTypeName { get; set; } = string.Empty;
    public string LenderName { get; set; } = string.Empty;
    public string? LoanAccountNumber { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal OutstandingPrincipal { get; set; }
    public decimal InterestRate { get; set; }
    public InterestType InterestType { get; set; }
    public int TenureMonths { get; set; }
    public int RemainingTenureMonths { get; set; }
    public decimal EMI { get; set; }
    public int EMIDayOfMonth { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime MaturityDate { get; set; }
    public LoanStatus Status { get; set; }
    public string Currency { get; set; } = "INR";
    public decimal TotalInterestPayable { get; set; }
    public decimal TotalAmountPayable { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalInterestPaid { get; set; }
    public DateTime? NextEMIDate { get; set; }
}

public class CreateLoanRequest
{
    public long UserId { get; set; }
    public long LoanTypeId { get; set; }
    public long? AccountId { get; set; }
    public string LenderName { get; set; } = string.Empty;
    public string? LoanAccountNumber { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestRate { get; set; }
    public InterestType InterestType { get; set; }
    public int TenureMonths { get; set; }
    public int EMIDayOfMonth { get; set; } = 1;
    public DateTime StartDate { get; set; }
    public DateTime? DisbursementDate { get; set; }
    public decimal ProcessingFee { get; set; }
    public decimal PrepaymentPenaltyPct { get; set; }
    public bool IsPrepaymentAllowed { get; set; } = true;
    public string Currency { get; set; } = "INR";
    public string? Notes { get; set; }
}

public class LoanListDto
{
    public long Id { get; set; }
        public string LoanTypeName { get; set; } = string.Empty;
public string LenderName { get; set; } = string.Empty;
    public decimal PrincipalAmount { get; set; }
    public decimal OutstandingPrincipal { get; set; }
    public decimal EMI { get; set; }
    public decimal InterestRate { get; set; }
    public int RemainingTenureMonths { get; set; }
    public LoanStatus Status { get; set; }
    public DateTime? NextEMIDate { get; set; }
}
