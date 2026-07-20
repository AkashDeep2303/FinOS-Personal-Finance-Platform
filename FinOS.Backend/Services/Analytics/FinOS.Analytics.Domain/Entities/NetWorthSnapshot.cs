namespace FinOS.Analytics.Domain.Entities;

public class NetWorthSnapshot
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public DateTime SnapshotDate { get; set; }
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal NetWorth { get; set; }
    public decimal CashAndBank { get; set; }
    public decimal InvestmentValue { get; set; }
    public decimal RealEstateValue { get; set; }
    public decimal GoldValue { get; set; }
    public decimal OtherAssets { get; set; }
    public decimal LoanOutstanding { get; set; }
    public decimal CreditCardOutstanding { get; set; }
    public decimal OtherLiabilities { get; set; }
    public decimal? ChangeFromPrevious { get; set; }
    public decimal? ChangePctFromPrevious { get; set; }
    public DateTime CreatedAt { get; set; }
}
