using FinOS.Common.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace FinOS.Common.Data;

/// <summary>
/// Factory that creates <see cref="SqlConnection"/> instances from the
/// connection string stored in <c>ConnectionStrings:DefaultConnection</c>.
/// </summary>
public sealed class SqlConnectionFactory : IConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Database connection string 'DefaultConnection' is not configured. " +
                "Add it under 'ConnectionStrings' in appsettings.json.");
        }

        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    /// <inheritdoc />
    public async Task<SqlConnection> CreateOpenConnectionAsync()
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }
}
