namespace FinOS.CoreFinance.Application.DTOs;

public class CategoryDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public decimal BudgetAmount { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public string CashFlowClassification { get; set; } = "Other";
    public DateTime CreatedAt { get; set; }
    public List<CategoryDto> Children { get; set; } = new();
}

public class CreateCategoryRequest
{
    public long? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Expense";
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public decimal BudgetAmount { get; set; }
    public int SortOrder { get; set; }
    public string CashFlowClassification { get; set; } = "Other";
}

public class UpdateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public decimal BudgetAmount { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public string CashFlowClassification { get; set; } = "Other";
}
