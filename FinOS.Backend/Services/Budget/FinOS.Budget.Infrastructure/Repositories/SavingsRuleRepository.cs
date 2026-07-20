using System.Data;
using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.Budget.Domain.Entities;
using FinOS.Budget.Domain.Interfaces;

namespace FinOS.Budget.Infrastructure.Repositories;

public class SavingsRuleRepository : ISavingsRuleRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public SavingsRuleRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<SavingsRule?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<SavingsRule>(
            "SELECT * FROM Budget.SavingsRules WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<PagedResult<SavingsRule>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause)
            ? ""
            : $"WHERE {whereClause}";
        var sortDirection = query.SortDirection?.ToLower() == "asc" ? "ASC" : "DESC";
        var sortColumn = !string.IsNullOrWhiteSpace(query.SortBy) ? query.SortBy : "CreatedAt";
        var offset = (query.PageNumber - 1) * query.PageSize;

        var countSql = $"SELECT COUNT(1) FROM [{schema}].[{tableName}] {where}";
        var dataSql = $"""
            SELECT * FROM [{schema}].[{tableName}] {where}
            ORDER BY [{sortColumn}] {sortDirection}
            OFFSET {offset} ROWS FETCH NEXT {query.PageSize} ROWS ONLY
            """;

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param);
        var items = (await connection.QueryAsync<SavingsRule>(dataSql, param)).ToList();

        return new PagedResult<SavingsRule>
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    public async Task<long> CountAsync(string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause)
            ? ""
            : $"WHERE {whereClause}";
        return await connection.ExecuteScalarAsync<long>(
            $"SELECT COUNT(1) FROM [{schema}].[{tableName}] {where}", param);
    }

    public async Task<List<SavingsRule>> GetByUserIdAsync(long userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<SavingsRule>(
            "SELECT * FROM Budget.SavingsRules WHERE UserId = @UserId ORDER BY CreatedAt DESC",
            new { UserId = userId });
        return result.ToList();
    }

    public async Task<List<SavingsRule>> GetActiveByUserIdAsync(long userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<SavingsRule>(
            "SELECT * FROM Budget.SavingsRules WHERE UserId = @UserId AND IsActive = 1 ORDER BY CreatedAt DESC",
            new { UserId = userId });
        return result.ToList();
    }

    public async Task<long> CreateAsync(SavingsRule entity, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var now = DateTime.UtcNow;
        var sql = @"
            INSERT INTO Budget.SavingsRules
                (UserId, RuleType, Name, TargetAccountId, SourceAccountId,
                 RoundUpTo, Percentage, FixedAmount, Frequency, DayOfMonth,
                 IsActive, TotalSaved, CreatedAt, UpdatedAt)
            VALUES
                (@UserId, @RuleType, @Name, @TargetAccountId, @SourceAccountId,
                 @RoundUpTo, @Percentage, @FixedAmount, @Frequency, @DayOfMonth,
                 @IsActive, @TotalSaved, @CreatedAt, @UpdatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

        var id = await connection.ExecuteScalarAsync<long>(sql, new
        {
            entity.UserId,
            RuleType = entity.RuleType.ToString(),
            entity.Name,
            entity.TargetAccountId,
            entity.SourceAccountId,
            entity.RoundUpTo,
            entity.Percentage,
            entity.FixedAmount,
            Frequency = entity.Frequency.ToString(),
            entity.DayOfMonth,
            entity.IsActive,
            entity.TotalSaved,
            CreatedAt = now,
            UpdatedAt = now
        });

        entity.Id = id;
        entity.CreatedAt = now;
        entity.UpdatedAt = now;
        return id;
    }


    public async Task<SavingsRule> AddAsync(SavingsRule entity, CancellationToken ct = default)
    {
        var id = await CreateAsync(entity, ct);
        entity.Id = id;
        return entity;
    }
    public async Task UpdateAsync(SavingsRule entity, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var now = DateTime.UtcNow;
        var sql = @"
            UPDATE Budget.SavingsRules
            SET Name = @Name,
                TargetAccountId = @TargetAccountId,
                SourceAccountId = @SourceAccountId,
                RoundUpTo = @RoundUpTo,
                Percentage = @Percentage,
                FixedAmount = @FixedAmount,
                Frequency = @Frequency,
                DayOfMonth = @DayOfMonth,
                IsActive = @IsActive,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id";

        await connection.ExecuteAsync(sql, new
        {
            entity.Id,
            entity.Name,
            entity.TargetAccountId,
            entity.SourceAccountId,
            entity.RoundUpTo,
            entity.Percentage,
            entity.FixedAmount,
            Frequency = entity.Frequency.ToString(),
            entity.DayOfMonth,
            entity.IsActive,
            UpdatedAt = now
        });

        entity.UpdatedAt = now;
    }


    public Task RemoveAsync(SavingsRule entity, CancellationToken ct = default) => Task.CompletedTask;
}
