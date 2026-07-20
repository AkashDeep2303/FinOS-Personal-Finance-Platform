using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.Goals.Domain.Entities;
using FinOS.Goals.Domain.Interfaces;

namespace FinOS.Goals.Infrastructure.Repositories;

public class GoalTemplateRepository : IGoalTemplateRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public GoalTemplateRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<GoalTemplate?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<GoalTemplate>(
            "SELECT * FROM [Goals].[GoalTemplates] WHERE Id = @Id", new { Id = id });
    }

    public async Task<PagedResult<GoalTemplate>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $" WHERE {whereClause}";
        var countSql = $"SELECT COUNT(*) FROM [{schema}].[{tableName}]{where}";
        var dataSql = $"SELECT * FROM [{schema}].[{tableName}]{where} ORDER BY SortOrder ASC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var dp = new DynamicParameters(param);
        dp.Add("@Offset", (query.PageNumber - 1) * query.PageSize);
        dp.Add("@PageSize", query.PageSize);

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param);
        var items = await connection.QueryAsync<GoalTemplate>(dataSql, dp);

        return new PagedResult<GoalTemplate>
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

    public async Task<List<GoalTemplate>> GetAllAsync(CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<GoalTemplate>(
            "SELECT * FROM [Goals].[GoalTemplates] ORDER BY SortOrder");
        return result.ToList();
    }

    public async Task<List<GoalTemplate>> ListAsync(string? category = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = category is null
            ? "SELECT * FROM [Goals].[GoalTemplates] ORDER BY SortOrder"
            : "SELECT * FROM [Goals].[GoalTemplates] WHERE Category = @Category ORDER BY SortOrder";
        var result = await connection.QueryAsync<GoalTemplate>(sql, new { Category = category });
        return result.ToList();
    }

    public Task<GoalTemplate> AddAsync(GoalTemplate entity, CancellationToken ct = default)
    {
        throw new NotImplementedException("Goal templates are seeded and not created at runtime.");
    }

    public Task UpdateAsync(GoalTemplate entity, CancellationToken ct = default) => Task.CompletedTask;

    public Task RemoveAsync(GoalTemplate entity, CancellationToken ct = default) => Task.CompletedTask;
}
