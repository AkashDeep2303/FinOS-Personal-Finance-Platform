namespace FinOS.CoreFinance.Domain.Entities;

public class Tag
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<TransactionTag> TransactionTags { get; set; } = new List<TransactionTag>();
}
