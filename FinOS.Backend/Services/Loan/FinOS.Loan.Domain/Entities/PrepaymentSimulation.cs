using FinOS.Loan.Domain.Enums;

namespace FinOS.Loan.Domain.Entities;

public class PrepaymentSimulation
{
    public long Id { get; set; }
    public long LoanId { get; set; }
    public long UserId { get; set; }
    public string SimulationName { get; set; } = string.Empty;
    public decimal PrepaymentAmount { get; set; }
    public DateTime PrepaymentDate { get; set; }
    public PrepaymentStrategy Strategy { get; set; }
    public int OriginalTenureMonths { get; set; }
    public int NewTenureMonths { get; set; }
    public int TenureSaved { get; set; }
    public decimal OriginalTotalInterest { get; set; }
    public decimal NewTotalInterest { get; set; }
    public decimal InterestSaved { get; set; }
    public decimal OriginalEMI { get; set; }
    public decimal NewEMI { get; set; }
    public decimal PenaltyEstimate { get; set; }
    public DateTime CreatedAt { get; set; }
}
