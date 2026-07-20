using Microsoft.Data.SqlClient;

namespace FinOS.Common.Interfaces;

/// <summary>
/// Represents a unit of work that wraps a single database connection and optional transaction.
/// Implements Dispose to clean up both the transaction and connection.
/// There is NO SaveChangesAsync — persistence is driven through CommitTransactionAsync.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// The active database connection for this unit of work.
    /// </summary>
    SqlConnection Connection { get; }

    /// <summary>
    /// The active transaction, or <c>null</c> if none has been started.
    /// </summary>
    SqlTransaction? Transaction { get; }

    /// <summary>
    /// Opens the underlying connection (if not already open) and begins a new transaction.
    /// Throws if a transaction is already in progress.
    /// </summary>
    Task BeginTransactionAsync();

    /// <summary>
    /// Commits the current transaction. Throws if no transaction is active.
    /// </summary>
    Task CommitTransactionAsync();

    /// <summary>
    /// Rolls back the current transaction. Throws if no transaction is active.
    /// </summary>
    Task RollbackTransactionAsync();

    /// <summary>
    /// NO-OP under the Dapper-based data access pattern. Repositories persist inline via
    /// their own async methods (e.g. <c>CreateAsync</c>, <c>UpdateAsync</c>, <c>DeleteAsync</c>),
    /// so there is no pending change-set to save. This method exists for backward
    /// compatibility with command handlers originally written for EF Core.
    /// New code should call the repository's specific async method directly and skip this call.
    /// </summary>
    /// <returns>Always <c>0</c>.</returns>
    Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
}
