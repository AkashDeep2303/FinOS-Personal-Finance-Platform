using FinOS.Investment.Domain.Enums;

namespace FinOS.Investment.Application.DTOs;

public class InvestmentTransactionDto
{
    public long Id { get; set; }
    public long HoldingId { get; set; }
    public TransactionType TransactionType { get; set; }
    public string TransactionTypeDisplay { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal PricePerUnit { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal Charges { get; set; }
    public decimal STT { get; set; }
    public decimal StampDuty { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Notes { get; set; }
}

public class RecordTransactionRequest
{
    public long HoldingId { get; set; }
    public TransactionType TransactionType { get; set; }
    public decimal Quantity { get; set; }
    public decimal PricePerUnit { get; set; }
    public decimal Charges { get; set; }
    public decimal STT { get; set; }
    public decimal StampDuty { get; set; }
    public DateTime TransactionDate { get; set; }
    public long? SourceAccountId { get; set; }
    public string? Notes { get; set; }
}
