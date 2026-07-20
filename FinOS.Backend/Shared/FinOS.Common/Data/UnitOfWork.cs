using FinOS.Common.Interfaces;
using Microsoft.Data.SqlClient;

namespace FinOS.Common.Data;

/// <summary>
/// Implementation of <see cref="IUnitOfWork"/> that owns a single <see cref="SqlConnection"/>
/// and an optional <see cref="SqlTransaction"/>. The connection is opened on first access.
/// Call <see cref="BeginTransactionAsync"/> to start a transaction, then
/// <see cref="CommitTransactionAsync"/> or <see cref="RollbackTransactionAsync"/> to finish it.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly IConnectionFactory _connectionFactory;
    private SqlConnection? _connection;
    private SqlTransaction? _transaction;
    private bool _disposed;

    public UnitOfWork(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    /// <inheritdoc />
    public SqlConnection Connection
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _connection ??= _connectionFactory.CreateConnection();
            return _connection;
        }
    }

    /// <inheritdoc />
    public SqlTransaction? Transaction
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _transaction;
        }
    }

    /// <inheritdoc />
    public async Task BeginTransactionAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_transaction is not null)
        {
            throw new InvalidOperationException(
                "A transaction is already in progress for this unit of work. " +
                "Commit or rollback the current transaction before starting a new one.");
        }

        if (_connection is null)
        {
            _connection = _connectionFactory.CreateConnection();
        }

        if (_connection.State != System.Data.ConnectionState.Open)
        {
            await _connection.OpenAsync();
        }

        _transaction = _connection.BeginTransaction();
    }

    /// <inheritdoc />
    public async Task CommitTransactionAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_transaction is null)
        {
            throw new InvalidOperationException(
                "Cannot commit — no transaction is in progress. " +
                "Call BeginTransactionAsync first.");
        }

        await _transaction.CommitAsync();
        await CleanupTransactionAsync();
    }

    /// <inheritdoc />
    public async Task RollbackTransactionAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_transaction is null)
        {
            throw new InvalidOperationException(
                "Cannot rollback — no transaction is in progress. " +
                "Call BeginTransactionAsync first.");
        }

        await _transaction.RollbackAsync();
        await CleanupTransactionAsync();
    }

    /// <summary>
    /// Disposes the transaction and connection in the correct order.
    /// If a transaction is still active, it is rolled back before disposal.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        if (_transaction is not null)
        {
            try
            {
                await _transaction.RollbackAsync();
            }
            catch (InvalidOperationException)
            {
                // Transaction may have already been committed or rolled back — ignore.
            }

            await CleanupTransactionAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }

        _disposed = true;
    }

    /// <summary>
    /// Synchronous dispose — delegates to <see cref="DisposeAsync"/>.
    /// </summary>
    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }

    // ── Private helpers ──────────────────────────────────────────────────

    private async Task CleanupTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
