using FinOS.Common.Interfaces;
using FinOS.Goals.Domain.Enums;

namespace FinOS.Goals.Domain.Entities;

public class Goal : IAuditableEntity, ISoftDeletable
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long? GoalTemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal MonthlyContribution { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? TargetDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public GoalPriority Priority { get; set; }
    public GoalStatus Status { get; set; }
    public string? LinkedAccountIds { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public bool IsAutoContribute { get; set; }
    public DateTime? ProjectedDate { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation
    public GoalTemplate? GoalTemplate { get; set; }
    public ICollection<GoalContribution> Contributions { get; set; } = new List<GoalContribution>();
}
