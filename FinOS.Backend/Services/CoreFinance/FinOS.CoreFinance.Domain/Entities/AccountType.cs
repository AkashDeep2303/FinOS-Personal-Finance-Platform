namespace FinOS.CoreFinance.Domain.Entities;

public class AccountType
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
