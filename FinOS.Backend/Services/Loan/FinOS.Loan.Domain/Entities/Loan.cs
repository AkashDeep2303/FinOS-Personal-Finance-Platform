using FinOS.Loan.Domain.Enums;
using FinOS.Common.Interfaces;

namespace FinOS.Loan.Domain.Entities;

public class Loan : IAuditableEntity, ISoftDeletable
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long LoanTypeId { get; set; }
    public long? AccountId { get; set; }
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
    public DateTime? DisbursementDate { get; set; }
    public decimal ProcessingFee { get; set; }
    public decimal PrepaymentPenaltyPct { get; set; }
    public bool IsPrepaymentAllowed { get; set; } = true;
    public decimal TotalInterestPayable { get; set; }
    public decimal TotalAmountPayable { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalInterestPaid { get; set; }
    public decimal TotalPrepaid { get; set; }
    public DateTime? NextEMIDate { get; set; }
    public LoanStatus Status { get; set; } = LoanStatus.Active;
    public string Currency { get; set; } = "INR";
    public string? Notes { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation
    public LoanType LoanType { get; set; } = null!;
    public List<EMISchedule> EMISchedule { get; set; } = new();
    public List<LoanPrepayment> Prepayments { get; set; } = new();
}
