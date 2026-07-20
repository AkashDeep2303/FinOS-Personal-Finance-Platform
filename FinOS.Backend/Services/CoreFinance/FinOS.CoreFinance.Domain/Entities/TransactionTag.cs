namespace FinOS.CoreFinance.Domain.Entities;

public class TransactionTag
{
    public long TransactionId { get; set; }
    public long TagId { get; set; }

    // Navigation
    public Transaction? Transaction { get; set; }
    public Tag? Tag { get; set; }
}
