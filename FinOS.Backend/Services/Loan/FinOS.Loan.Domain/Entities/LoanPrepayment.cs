using FinOS.Loan.Domain.Enums;

namespace FinOS.Loan.Domain.Entities;

public class LoanPrepayment
{
    public long Id { get; set; }
    public long LoanId { get; set; }
    public DateTime PrepaymentDate { get; set; }
    public decimal PrepaymentAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public PrepaymentType PrepaymentType { get; set; }
    public int? TenureReduction { get; set; }
    public decimal InterestSaved { get; set; }
    public decimal NewOutstanding { get; set; }
    public decimal? NewEMI { get; set; }
    public int? NewTenureMonths { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Loan Loan { get; set; } = null!;
}
