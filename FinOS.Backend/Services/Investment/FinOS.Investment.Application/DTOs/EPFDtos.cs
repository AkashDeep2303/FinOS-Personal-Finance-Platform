namespace FinOS.Investment.Application.DTOs;

public class EPFAccountDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string? UAN { get; set; }
    public string? EstablishmentCode { get; set; }
    public string? EmployerName { get; set; }
    public decimal EmployeeContributionPct { get; set; }
    public decimal EmployerContributionPct { get; set; }
    public decimal EPSCorpus { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal MonthlySalary { get; set; }
    public decimal InterestRate { get; set; }
    public DateTime StartDate { get; set; }
    public bool IsActive { get; set; }
}

public class CreateEPFAccountRequest
{
    public long UserId { get; set; }
    public string? UAN { get; set; }
    public string? EstablishmentCode { get; set; }
    public string? EmployerName { get; set; }
    public decimal EmployeeContributionPct { get; set; } = 12m;
    public decimal EmployerContributionPct { get; set; } = 12m;
    public decimal MonthlySalary { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal InterestRate { get; set; } = 8.25m;
    public DateTime StartDate { get; set; }
}

public class UpdateEPFContributionRequest
{
    public long EPFAccountId { get; set; }
    public DateTime Month { get; set; }
    public decimal MonthlySalary { get; set; }
}

public class EPFContributionDto
{
    public long Id { get; set; }
    public DateTime Month { get; set; }
    public decimal EmployeeContribution { get; set; }
    public decimal EmployerContribution { get; set; }
    public decimal EPSContribution { get; set; }
    public decimal InterestEarned { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
}

public class EPFProjectionDto
{
    public decimal CurrentBalance { get; set; }
    public decimal MonthlyContribution { get; set; }
    public decimal InterestRate { get; set; }
    public int YearsToRetirement { get; set; }
    public decimal ProjectedCorpus { get; set; }
    public decimal TotalContributions { get; set; }
    public decimal TotalInterestEarned { get; set; }
    public List<YearlyProjectionDto> YearlyBreakdown { get; set; } = new();
}

public class YearlyProjectionDto
{
    public int Year { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal YearlyContribution { get; set; }
    public decimal InterestEarned { get; set; }
    public decimal ClosingBalance { get; set; }
}
