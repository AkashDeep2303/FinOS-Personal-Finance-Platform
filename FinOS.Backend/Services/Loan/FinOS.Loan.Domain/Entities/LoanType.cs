namespace FinOS.Loan.Domain.Entities;

public class LoanType
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
}
