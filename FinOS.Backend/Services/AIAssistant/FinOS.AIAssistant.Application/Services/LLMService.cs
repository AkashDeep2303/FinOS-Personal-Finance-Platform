using FinOS.AIAssistant.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace FinOS.AIAssistant.Application.Services;

public interface ILLMService
{
    Task<LLMResponse> SendMessageAsync(long userId, string userMessage, QueryType queryType, string? conversationContext, CancellationToken ct = default);
}

public record LLMResponse(string Content, int TokenCount, long ResponseTimeMs);

public class LLMService : ILLMService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LLMService> _logger;

    public LLMService(HttpClient httpClient, ILogger<LLMService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<LLMResponse> SendMessageAsync(long userId, string userMessage, QueryType queryType, string? conversationContext, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var systemPrompt = BuildSystemPrompt(queryType);
            var fullPrompt = conversationContext is not null
                ? $"{systemPrompt}\n\nConversation context:\n{conversationContext}\n\nUser: {userMessage}"
                : $"{systemPrompt}\n\nUser: {userMessage}";

            // In production, this calls the z-ai-web-dev-sdk or OpenAI-compatible endpoint
            // For now, generate a contextual response based on query type
            var response = GenerateContextualResponse(queryType, userMessage);
            var elapsed = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

            return new LLMResponse(response, EstimateTokenCount(fullPrompt + response), elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling LLM service for user {UserId}", userId);
            var elapsed = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            return new LLMResponse(
                "I'm sorry, I'm currently unable to process your request. Please try again later.",
                0, elapsed);
        }
    }

    private static string BuildSystemPrompt(QueryType queryType) => queryType switch
    {
        QueryType.Affordability => "You are a financial advisor helping users determine if they can afford a purchase. Analyze their income, expenses, savings, and financial goals to provide a recommendation. Be specific with numbers and percentages.",
        QueryType.SpendingAnalysis => "You are a financial analyst helping users understand their spending patterns. Identify trends, anomalies, and areas for improvement. Provide actionable insights with specific categories and amounts.",
        QueryType.LoanPrepayment => "You are a loan advisor helping users decide whether to prepay their loans. Consider interest savings, opportunity cost, emergency fund adequacy, and tax implications. Provide calculations and comparisons.",
        QueryType.InvestmentAdvice => "You are an investment advisor providing general investment guidance. Consider risk tolerance, time horizon, diversification, and asset allocation. Note: This is educational, not specific investment advice.",
        _ => "You are a helpful personal finance assistant. Help users with their financial questions by providing clear, actionable advice. Always consider their overall financial health when making recommendations."
    };

    private static string GenerateContextualResponse(QueryType queryType, string userMessage)
    {
        return queryType switch
        {
            QueryType.Affordability => $"Based on your question about affordability: \"{userMessage}\"\n\nTo properly assess this, I'd need to review your current income, fixed expenses, savings rate, and existing financial commitments. As a general rule, ensure that any new commitment doesn't push your savings rate below 20% and your debt-to-income ratio above 30%.",
            QueryType.SpendingAnalysis => $"Regarding your spending analysis request: \"{userMessage}\"\n\nI can help identify patterns in your spending. Key areas to examine include: top spending categories, month-over-month trends, unusual transactions, and opportunities to optimize. Would you like me to focus on any specific category or time period?",
            QueryType.LoanPrepayment => $"For your loan prepayment question: \"{userMessage}\"\n\nKey factors to consider: 1) Interest rate on the loan vs potential investment returns, 2) Emergency fund adequacy (6+ months recommended), 3) Tax benefits of loan interest, 4) Prepayment penalties. Generally, prepay high-interest loans first while maintaining an adequate emergency fund.",
            QueryType.InvestmentAdvice => $"Regarding your investment question: \"{userMessage}\"\n\nImportant considerations: 1) Your risk tolerance and investment timeline, 2) Diversification across asset classes, 3) Emergency fund before aggressive investing, 4) Tax-advantaged accounts. Remember to review and rebalance your portfolio periodically.",
            _ => $"Thank you for your question: \"{userMessage}\"\n\nI'm here to help with your personal finance questions. I can assist with affordability checks, spending analysis, loan prepayment decisions, and investment guidance. Could you provide more details so I can give you a more tailored response?"
        };
    }

    private static int EstimateTokenCount(string text) => text.Length / 4;
}
