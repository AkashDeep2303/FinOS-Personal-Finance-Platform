namespace FinOS.Goals.Domain.Entities;

public class GoalTemplate
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal SuggestedAmount { get; set; }
    public int SuggestedMonths { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public int SortOrder { get; set; }
}
