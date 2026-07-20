using FinOS.Investment.Domain.Enums;

namespace FinOS.Investment.Domain.Entities;

public class GoldPriceHistory
{
    public long Id { get; set; }
    public DateTime PriceDate { get; set; }
    public GoldType GoldType { get; set; }
    public decimal PricePer10g { get; set; }
    public DateTime CreatedAt { get; set; }
}
