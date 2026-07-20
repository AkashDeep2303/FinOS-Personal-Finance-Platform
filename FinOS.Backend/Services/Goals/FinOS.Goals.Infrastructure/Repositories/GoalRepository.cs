using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.Goals.Domain.Entities;
using FinOS.Goals.Domain.Interfaces;

namespace FinOS.Goals.Infrastructure.Repositories;

public class GoalRepository : IGoalRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public GoalRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Goal?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Goal>(
            "SELECT * FROM [Goals].[Goals] WHERE Id = @Id AND DeletedAt IS NULL", new { Id = id });
    }

    public async Task<PagedResult<Goal>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "WHERE DeletedAt IS NULL" : $"WHERE DeletedAt IS NULL AND ({whereClause})";
        var countSql = $"SELECT COUNT(*) FROM [{schema}].[{tableName}] {where}";
        var dataSql = $"SELECT * FROM [{schema}].[{tableName}] {where} ORDER BY Priority DESC, CreatedAt DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var dp = new DynamicParameters(param);
        dp.Add("@Offset", (query.PageNumber - 1) * query.PageSize);
        dp.Add("@PageSize", query.PageSize);

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param);
        var items = await connection.QueryAsync<Goal>(dataSql, dp);

        return new PagedResult<Goal>
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
        var where = string.IsNullOrWhiteSpace(whereClause) ? "WHERE DeletedAt IS NULL" : $"WHERE DeletedAt IS NULL AND ({whereClause})";
        return await connection.ExecuteScalarAsync<long>($"SELECT COUNT(*) FROM [{schema}].[{tableName}] {where}", param);
    }

    public async Task<List<Goal>> GetByUserIdAsync(long userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<Goal>(
            "SELECT * FROM [Goals].[Goals] WHERE UserId = @UserId AND DeletedAt IS NULL ORDER BY Priority DESC, CreatedAt DESC",
            new { UserId = userId });
        return result.ToList();
    }

    public async Task<Goal?> GetWithContributionsAsync(long goalId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT * FROM [Goals].[Goals] WHERE Id = @GoalId AND DeletedAt IS NULL;
            SELECT * FROM [Goals].[GoalContributions] WHERE GoalId = @GoalId ORDER BY ContributionDate DESC;";

        using var multi = await connection.QueryMultipleAsync(sql, new { GoalId = goalId });
        var goal = await multi.ReadFirstOrDefaultAsync<Goal>();
        if (goal != null)
        {
            var contributions = (await multi.ReadAsync<GoalContribution>()).ToList();
            goal.Contributions = contributions;
        }
        return goal;
    }

    public async Task<long> CreateGoalAsync(Goal goal, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", goal.UserId);
        parameters.Add("@GoalTemplateId", goal.GoalTemplateId);
        parameters.Add("@Name", goal.Name);
        parameters.Add("@Description", goal.Description);
        parameters.Add("@Category", goal.Category);
        parameters.Add("@TargetAmount", goal.TargetAmount);
        parameters.Add("@CurrentAmount", goal.CurrentAmount);
        parameters.Add("@MonthlyContribution", goal.MonthlyContribution);
        parameters.Add("@StartDate", goal.StartDate);
        parameters.Add("@TargetDate", goal.TargetDate);
        parameters.Add("@Priority", goal.Priority.ToString());
        parameters.Add("@LinkedAccountIds", goal.LinkedAccountIds);
        parameters.Add("@Icon", goal.Icon);
        parameters.Add("@Color", goal.Color);
        parameters.Add("@IsAutoContribute", goal.IsAutoContribute);
        parameters.Add("@NewGoalId", dbType: System.Data.DbType.Int64, direction: System.Data.ParameterDirection.Output);

        await connection.ExecuteAsync(
            "Goals.sp_CreateGoal", parameters,
            commandType: System.Data.CommandType.StoredProcedure);

        return parameters.Get<long>("@NewGoalId");
    }
    public async Task UpdateGoalAsync(long goalId, string? name, string? description, decimal? targetAmount, DateTime? targetDate, string? category, string? priority, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "Goals.sp_UpdateGoal",
            new { GoalId = goalId, Name = name, Description = description, TargetAmount = targetAmount, TargetDate = targetDate, Category = category, Priority = priority },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    public async Task PauseGoalAsync(long goalId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "Goals.sp_PauseGoal",
            new { GoalId = goalId },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    public async Task ResumeGoalAsync(long goalId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "Goals.sp_ResumeGoal",
            new { GoalId = goalId },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    public async Task<Goal> AddAsync(Goal entity, CancellationToken ct = default)
    {
        var id = await CreateGoalAsync(entity, ct);
        entity.Id = id;
        return entity;
    }

    public async Task UpdateAsync(Goal entity, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            UPDATE [Goals].[Goals]
            SET CurrentAmount = @CurrentAmount,
                Status = @Status,
                CompletedDate = @CompletedDate,
                IsAutoContribute = @IsAutoContribute,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id AND DeletedAt IS NULL",
            new
            {
                entity.Id,
                entity.CurrentAmount,
                Status = entity.Status.ToString(),
                entity.CompletedDate,
                entity.IsAutoContribute,
                entity.UpdatedAt
            });
    }

    public async Task SoftDeleteAsync(long goalId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "UPDATE [Goals].[Goals] SET DeletedAt = SYSUTCDATETIME(), UpdatedAt = SYSUTCDATETIME() WHERE Id = @GoalId AND DeletedAt IS NULL",
            new { GoalId = goalId });
    }

    public Task RemoveAsync(Goal entity, CancellationToken ct = default) => Task.CompletedTask;
}
