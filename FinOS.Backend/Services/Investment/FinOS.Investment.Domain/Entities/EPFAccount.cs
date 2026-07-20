using FinOS.Common.Interfaces;

namespace FinOS.Investment.Domain.Entities;

public class EPFAccount : IAuditableEntity
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string? UAN { get; set; }
    public string? EstablishmentCode { get; set; }
    public string? EmployerName { get; set; }
    public decimal EmployeeContributionPct { get; set; } = 12m;
    public decimal EmployerContributionPct { get; set; } = 12m;
    public decimal EPSCorpus { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal MonthlySalary { get; set; }
    public decimal InterestRate { get; set; } = 8.25m;
    public DateTime StartDate { get; set; }
    public bool IsActive { get; set; } = true;

    // Audit
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation
    public List<EPFContribution> Contributions { get; set; } = new();
}
