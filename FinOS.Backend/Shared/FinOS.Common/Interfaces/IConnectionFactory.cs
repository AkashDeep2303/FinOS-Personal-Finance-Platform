using Microsoft.Data.SqlClient;

namespace FinOS.Common.Interfaces;

/// <summary>
/// Creates new SQL database connections from the configured connection string.
/// Each call returns a fresh, un-opened SqlConnection that the caller owns and must dispose.
/// </summary>
public interface IConnectionFactory
{
    /// <summary>
    /// Creates a new <see cref="SqlConnection"/> using the configured connection string.
    /// The connection is NOT opened — the caller is responsible for opening and disposing it.
    /// </summary>
    SqlConnection CreateConnection();

    /// <summary>
    /// Creates and opens a new <see cref="SqlConnection"/> ready for immediate use.
    /// The caller is responsible for disposing the connection.
    /// </summary>
    Task<SqlConnection> CreateOpenConnectionAsync();
}
