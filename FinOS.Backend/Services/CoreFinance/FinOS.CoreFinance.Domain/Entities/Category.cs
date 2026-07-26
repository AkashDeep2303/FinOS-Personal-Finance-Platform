using FinOS.Common.Interfaces;
using FinOS.CoreFinance.Domain.Enums;

namespace FinOS.CoreFinance.Domain.Entities;

public class Category : IAuditableEntity
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public CategoryType Type { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public decimal BudgetAmount { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public string CashFlowClassification { get; set; } = "Other";

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Self-referencing navigation
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
