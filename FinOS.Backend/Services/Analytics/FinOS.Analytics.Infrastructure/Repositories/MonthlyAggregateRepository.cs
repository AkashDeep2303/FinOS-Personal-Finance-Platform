using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.Analytics.Domain.Entities;
using FinOS.Analytics.Domain.Interfaces;

namespace FinOS.Analytics.Infrastructure.Repositories;

public class MonthlyAggregateRepository : IMonthlyAggregateRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public MonthlyAggregateRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<MonthlyAggregate?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<MonthlyAggregate>(
            "SELECT * FROM [Analytics].[MonthlyAggregates] WHERE Id = @Id", new { Id = id });
    }

    public async Task<PagedResult<MonthlyAggregate>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $" WHERE {whereClause}";
        var countSql = $"SELECT COUNT(*) FROM [{schema}].[{tableName}]{where}";
        var dataSql = $"SELECT * FROM [{schema}].[{tableName}]{where} ORDER BY YearMonth DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var dp = new DynamicParameters(param);
        dp.Add("@Offset", (query.PageNumber - 1) * query.PageSize);
        dp.Add("@PageSize", query.PageSize);

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param);
        var items = await connection.QueryAsync<MonthlyAggregate>(dataSql, dp);

        return new PagedResult<MonthlyAggregate>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            Page = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    public async Task<long> CountAsync(string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $" WHERE {whereClause}";
        return await connection.ExecuteScalarAsync<long>($"SELECT COUNT(*) FROM [{schema}].[{tableName}]{where}", param);
    }

    public async Task<List<MonthlyAggregate>> GetByUserAsync(long userId, int months, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<MonthlyAggregate>(
            @"SELECT * FROM [Analytics].[MonthlyAggregates] 
              WHERE UserId = @UserId AND YearMonth >= CONVERT(INT, CONVERT(VARCHAR(6), DATEADD(MONTH, -@Months, GETUTCDATE()), 112))
              ORDER BY YearMonth ASC",
            new { UserId = userId, Months = months });
        return result.ToList();
    }

    public async Task<MonthlyAggregate?> GetByUserAndMonthAsync(long userId, int yearMonth, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<MonthlyAggregate>(
            "SELECT * FROM [Analytics].[MonthlyAggregates] WHERE UserId = @UserId AND YearMonth = @YearMonth",
            new { UserId = userId, YearMonth = yearMonth });
    }

    public async Task GenerateMonthlyAggregatesAsync(long userId, int yearMonth, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "Analytics.sp_GenerateMonthlyAggregates",
            new { UserId = userId, YearMonth = yearMonth },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    public async Task<MonthlyAggregate> AddAsync(MonthlyAggregate entity, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO [Analytics].[MonthlyAggregates] (UserId, YearMonth, TotalIncome, TotalExpense, TotalSavings, SavingsRate, TopExpenseCategory, TopExpenseAmount, TransactionCount, CategoryBreakdown, CreatedAt, UpdatedAt)
            VALUES (@UserId, @YearMonth, @TotalIncome, @TotalExpense, @TotalSavings, @SavingsRate, @TopExpenseCategory, @TopExpenseAmount, @TransactionCount, @CategoryBreakdown, @CreatedAt, @UpdatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
        var id = await connection.ExecuteScalarAsync<long>(sql, entity);
        entity.Id = id;
        return entity;
    }

    public Task UpdateAsync(MonthlyAggregate entity, CancellationToken ct = default) => Task.CompletedTask;

    public Task RemoveAsync(MonthlyAggregate entity, CancellationToken ct = default) => Task.CompletedTask;
}
