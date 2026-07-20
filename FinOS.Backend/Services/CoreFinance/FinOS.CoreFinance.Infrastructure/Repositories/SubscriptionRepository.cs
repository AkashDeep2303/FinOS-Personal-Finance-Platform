using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.CoreFinance.Domain.Entities;
using FinOS.CoreFinance.Domain.Interfaces;

namespace FinOS.CoreFinance.Infrastructure.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public SubscriptionRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<DetectedSubscription?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT ds.*, c.*, t.*
            FROM Core.DetectedSubscriptions ds
            LEFT JOIN Core.Categories c ON ds.CategoryId = c.Id
            LEFT JOIN Core.Transactions t ON ds.LastTransactionId = t.Id
            WHERE ds.Id = @Id";
        var result = await connection.QueryAsync<DetectedSubscription, Category, Transaction, DetectedSubscription>(sql,
            (sub, category, transaction) => { sub.Category = category; sub.LastTransaction = transaction; return sub; },
            new { Id = id }, splitOn: "Id,Id");
        return result.FirstOrDefault();
    }

    public async Task<PagedResult<DetectedSubscription>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $"WHERE {whereClause}";
        var offset = (query.PageNumber - 1) * query.PageSize;
        var countSql = $"SELECT COUNT(1) FROM [{schema}].[{tableName}] {where}";
        var dataSql = $"""
            SELECT * FROM [{schema}].[{tableName}] {where}
            ORDER BY DetectionConfidence DESC
            OFFSET {offset} ROWS FETCH NEXT {query.PageSize} ROWS ONLY
            """;
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param);
        var items = (await connection.QueryAsync<DetectedSubscription>(dataSql, param)).ToList();
        return new PagedResult<DetectedSubscription> { Items = items, TotalCount = totalCount, Page = query.PageNumber, PageSize = query.PageSize };
    }

    public async Task<long> CountAsync(string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $"WHERE {whereClause}";
        return await connection.ExecuteScalarAsync<long>($"SELECT COUNT(1) FROM [{schema}].[{tableName}] {where}", param);
    }

    public async Task<List<DetectedSubscription>> GetByUserIdAsync(long userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<DetectedSubscription>(
            "SELECT * FROM Core.DetectedSubscriptions WHERE UserId = @UserId ORDER BY DetectionConfidence DESC",
            new { UserId = userId });
        return result.ToList();
    }

    public async Task<DetectedSubscription> AddAsync(DetectedSubscription entity, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO Core.DetectedSubscriptions (UserId, CategoryId, LastTransactionId, MerchantName, Amount, Currency, Frequency, NextExpectedDate, LastTransactionDate, DetectionConfidence, TransactionCount, IsConfirmed, IsActive, CreatedAt, UpdatedAt)
            VALUES (@UserId, @CategoryId, @LastTransactionId, @MerchantName, @Amount, @Currency, @Frequency, @NextExpectedDate, @LastTransactionDate, @DetectionConfidence, @TransactionCount, @IsConfirmed, @IsActive, @CreatedAt, @UpdatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
        var id = await connection.ExecuteScalarAsync<long>(sql, entity);
        entity.Id = id;
        return entity;
    }

    public Task UpdateAsync(DetectedSubscription entity, CancellationToken ct = default) => Task.CompletedTask;
    public Task RemoveAsync(DetectedSubscription entity, CancellationToken ct = default) => Task.CompletedTask;

    public async Task<List<DetectedSubscription>> DetectSubscriptionsAsync(long userId, CancellationToken ct = default)
    {
        // Placeholder: subscription detection logic would go here
        return await GetByUserIdAsync(userId, ct);
    }
}
