using FinOS.Loan.Domain.Enums;

namespace FinOS.Loan.Application.DTOs;

public class LoanPrepaymentDto
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
}

public class SimulatePrepaymentRequest
{
    public long LoanId { get; set; }
    public decimal PrepaymentAmount { get; set; }
    public DateTime PrepaymentDate { get; set; }
    public PrepaymentStrategy Strategy { get; set; }
    public string? SimulationName { get; set; }
}

public class PrepaymentSimulationDto
{
    public long Id { get; set; }
    public string SimulationName { get; set; } = string.Empty;
    public decimal PrepaymentAmount { get; set; }
    public PrepaymentStrategy Strategy { get; set; }
    public string StrategyDisplay { get; set; } = string.Empty;
    public int OriginalTenureMonths { get; set; }
    public int NewTenureMonths { get; set; }
    public int TenureSaved { get; set; }
    public decimal OriginalTotalInterest { get; set; }
    public decimal NewTotalInterest { get; set; }
    public decimal InterestSaved { get; set; }
    public decimal OriginalEMI { get; set; }
    public decimal NewEMI { get; set; }
    public decimal PenaltyEstimate { get; set; }
}

public class ExecutePrepaymentRequest
{
    public long LoanId { get; set; }
    public decimal PrepaymentAmount { get; set; }
    public DateTime PrepaymentDate { get; set; }
    public PrepaymentStrategy Strategy { get; set; }
    public string? Notes { get; set; }
}
