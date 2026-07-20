namespace FinOS.Goals.Domain.Entities;

public class GoalContribution
{
    public long Id { get; set; }
    public long GoalId { get; set; }
    public decimal Amount { get; set; }
    public DateTime ContributionDate { get; set; }
    public string Source { get; set; } = "Manual";
    public long? SourceAccountId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Goal? Goal { get; set; }
}
