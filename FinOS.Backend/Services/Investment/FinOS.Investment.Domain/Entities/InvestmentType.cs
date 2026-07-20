using FinOS.Investment.Domain.Enums;

namespace FinOS.Investment.Domain.Entities;

public class InvestmentType
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AssetClass AssetClass { get; set; }
    public string? Icon { get; set; }
    public bool IsTaxSaving { get; set; }
    public int SortOrder { get; set; }
}
