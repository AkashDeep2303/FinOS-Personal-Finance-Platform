using FinOS.Common.Extensions;
using FinOS.AIAssistant.Application.Services;
using FinOS.AIAssistant.Domain.Interfaces;
using FinOS.AIAssistant.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinOS.AIAssistant.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Repositories
        services.AddFinOSDataAccess();
        services.AddScoped<IAIConversationRepository, AIConversationRepository>();
        services.AddScoped<IAIMessageRepository, AIMessageRepository>();

        // Register application services
        services.AddScoped<ILLMService, LLMService>();
        services.AddHttpClient<ILLMService, LLMService>();

        return services;
    }
}
