using FinOS.Investment.Domain.Enums;

namespace FinOS.Investment.Domain.Entities;

public class InvestmentTransaction
{
    public long Id { get; set; }
    public long HoldingId { get; set; }
    public TransactionType TransactionType { get; set; }
    public decimal Quantity { get; set; }
    public decimal PricePerUnit { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal Charges { get; set; }
    public decimal STT { get; set; }
    public decimal StampDuty { get; set; }
    public DateTime TransactionDate { get; set; }
    public DateTime? SettlementDate { get; set; }
    public long? SIPId { get; set; }
    public string? Notes { get; set; }
    public long? SourceAccountId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Holding Holding { get; set; } = null!;
}
