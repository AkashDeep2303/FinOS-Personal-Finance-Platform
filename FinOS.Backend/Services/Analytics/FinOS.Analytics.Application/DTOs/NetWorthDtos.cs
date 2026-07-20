namespace FinOS.Analytics.Application.DTOs;

public record NetWorthDto(
    long Id,
    long UserId,
    DateTime SnapshotDate,
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal NetWorth,
    decimal CashAndBank,
    decimal InvestmentValue,
    decimal RealEstateValue,
    decimal GoldValue,
    decimal OtherAssets,
    decimal LoanOutstanding,
    decimal CreditCardOutstanding,
    decimal OtherLiabilities,
    decimal? ChangeFromPrevious,
    decimal? ChangePctFromPrevious,
    DateTime CreatedAt
);

public record CalculateNetWorthDto(
    long UserId,
    decimal CashAndBank,
    decimal InvestmentValue,
    decimal RealEstateValue,
    decimal GoldValue,
    decimal OtherAssets,
    decimal LoanOutstanding,
    decimal CreditCardOutstanding,
    decimal OtherLiabilities
);
