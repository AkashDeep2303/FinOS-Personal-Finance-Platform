using FinOS.Common.Interfaces;

namespace FinOS.Loan.Domain.Entities;

public class EMISchedule : IAuditableEntity
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
    public decimal? ActualPrincipalPaid { get; set; }
    public decimal? ActualInterestPaid { get; set; }
    public decimal LateFee { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation
    public Loan Loan { get; set; } = null!;
}
