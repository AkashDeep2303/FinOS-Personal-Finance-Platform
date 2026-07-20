using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.Goals.Domain.Entities;
using FinOS.Goals.Domain.Interfaces;

namespace FinOS.Goals.Infrastructure.Repositories;

public class GoalContributionRepository : IGoalContributionRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public GoalContributionRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<GoalContribution?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<GoalContribution>(
            "SELECT * FROM [Goals].[GoalContributions] WHERE Id = @Id", new { Id = id });
    }

    public async Task<PagedResult<GoalContribution>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $" WHERE {whereClause}";
        var countSql = $"SELECT COUNT(*) FROM [{schema}].[{tableName}]{where}";
        var dataSql = $"SELECT * FROM [{schema}].[{tableName}]{where} ORDER BY ContributionDate DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var dp = new DynamicParameters(param);
        dp.Add("@Offset", (query.PageNumber - 1) * query.PageSize);
        dp.Add("@PageSize", query.PageSize);

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param);
        var items = await connection.QueryAsync<GoalContribution>(dataSql, dp);

        return new PagedResult<GoalContribution>
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

    public async Task<List<GoalContribution>> GetByGoalIdAsync(long goalId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<GoalContribution>(
            "SELECT * FROM [Goals].[GoalContributions] WHERE GoalId = @GoalId ORDER BY ContributionDate DESC",
            new { GoalId = goalId });
        return result.ToList();
    }

    public async Task<long> AddGoalContributionAsync(long goalId, decimal amount, DateTime contributionDate, string? notes, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@GoalId", goalId);
        parameters.Add("@Amount", amount);
        parameters.Add("@ContributionDate", contributionDate);
        parameters.Add("@Notes", notes);

        await connection.ExecuteAsync(
            "Goals.sp_AddGoalContribution", parameters,
            commandType: System.Data.CommandType.StoredProcedure);

        return 0;
    }

    public async Task<GoalContribution> AddAsync(GoalContribution entity, CancellationToken ct = default)
    {
        var id = await AddGoalContributionAsync(entity.GoalId, entity.Amount, entity.ContributionDate, entity.Notes, ct);
        entity.Id = id;
        return entity;
    }

    public Task UpdateAsync(GoalContribution entity, CancellationToken ct = default) => Task.CompletedTask;

    public Task RemoveAsync(GoalContribution entity, CancellationToken ct = default) => Task.CompletedTask;
}
