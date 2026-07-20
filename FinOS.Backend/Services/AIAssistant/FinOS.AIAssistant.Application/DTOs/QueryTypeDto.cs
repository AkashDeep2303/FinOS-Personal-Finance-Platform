using FinOS.AIAssistant.Domain.Enums;

namespace FinOS.AIAssistant.Application.DTOs;

public record QueryTypeDto(
    QueryType Type,
    string DisplayName,
    string Description
);

public static class QueryTypeCatalog
{
    public static readonly List<QueryTypeDto> All = new()
    {
        new(QueryType.Affordability, "Affordability Check", "Can I afford this purchase?"),
        new(QueryType.SpendingAnalysis, "Spending Analysis", "Analyze my spending patterns"),
        new(QueryType.LoanPrepayment, "Loan Prepayment", "Should I prepay my loan?"),
        new(QueryType.InvestmentAdvice, "Investment Advice", "Get investment recommendations"),
        new(QueryType.General, "General", "General financial questions")
    };
}
