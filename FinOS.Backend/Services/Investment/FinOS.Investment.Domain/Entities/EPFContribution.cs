namespace FinOS.Investment.Domain.Entities;

public class EPFContribution
{
    public long Id { get; set; }
    public long EPFAccountId { get; set; }
    public DateTime Month { get; set; }
    public decimal EmployeeContribution { get; set; }
    public decimal EmployerContribution { get; set; }
    public decimal EPSContribution { get; set; }
    public decimal InterestEarned { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public EPFAccount EPFAccount { get; set; } = null!;
}
