using FinOS.Common.Data;
using FinOS.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FinOS.Common.Extensions;

/// <summary>
/// Extension methods for registering FinOS data-access services
/// into the ASP.NET Core dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core FinOS data-access services:
    /// <list type="bullet">
    ///   <item><see cref="IConnectionFactory"/> → <see cref="SqlConnectionFactory"/> (singleton)</item>
    ///   <item><see cref="IUnitOfWork"/> → <see cref="UnitOfWork"/> (scoped — one per HTTP request)</item>
    /// </list>
    /// <para>
    /// Requires a connection string named <c>DefaultConnection</c> in <c>ConnectionStrings</c>.
    /// </para>
    /// </summary>
    /// <param name="services">The service collection to register services into.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // In Program.cs:
    /// builder.Services.AddFinOSDataAccess();
    /// </code>
    /// </example>
    public static IServiceCollection AddFinOSDataAccess(this IServiceCollection services)
    {
        // ConnectionFactory is singleton — it only reads config once and
        // creates new SqlConnection instances on demand (connections are NOT shared).
        services.AddSingleton<IConnectionFactory, SqlConnectionFactory>();

        // UnitOfWork is scoped — one instance per HTTP request ensures
        // a single connection and optional transaction per request.
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    /// <summary>
    /// Registers FinOS data-access services with an explicit connection string,
    /// bypassing IConfiguration. Useful in testing or non-web scenarios.
    /// </summary>
    /// <param name="services">The service collection to register services into.</param>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFinOSDataAccess(this IServiceCollection services, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException(
                "Connection string cannot be null or whitespace.",
                nameof(connectionString));
        }

        // Register a factory that ignores IConfiguration and uses the provided string.
        services.AddSingleton<IConnectionFactory>(_ => new DirectConnectionFactory(connectionString));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    // ── Internal factory for explicit connection strings ─────────────────

    /// <summary>
    /// Lightweight <see cref="IConnectionFactory"/> that takes a connection string directly,
    /// bypassing <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>.
    /// </summary>
    private sealed class DirectConnectionFactory : IConnectionFactory
    {
        private readonly string _connectionString;

        public DirectConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Microsoft.Data.SqlClient.SqlConnection CreateConnection()
        {
            return new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
        }

        public async Task<Microsoft.Data.SqlClient.SqlConnection> CreateOpenConnectionAsync()
        {
            var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }
    }
}
