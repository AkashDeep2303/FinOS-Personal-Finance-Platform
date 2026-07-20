using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.Analytics.Domain.Entities;
using FinOS.Analytics.Domain.Interfaces;

namespace FinOS.Analytics.Infrastructure.Repositories;

public class FinancialScoreRepository : IFinancialScoreRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public FinancialScoreRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<FinancialScore?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<FinancialScore>(
            "SELECT * FROM [Analytics].[FinancialScore] WHERE Id = @Id", new { Id = id });
    }

    public async Task<PagedResult<FinancialScore>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $" WHERE {whereClause}";
        var countSql = $"SELECT COUNT(*) FROM [{schema}].[{tableName}]{where}";
        var dataSql = $"SELECT * FROM [{schema}].[{tableName}]{where} ORDER BY ScoreDate DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var dp = new DynamicParameters(param);
        dp.Add("@Offset", (query.PageNumber - 1) * query.PageSize);
        dp.Add("@PageSize", query.PageSize);

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param);
        var items = await connection.QueryAsync<FinancialScore>(dataSql, dp);

        return new PagedResult<FinancialScore>
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

    public async Task<List<FinancialScore>> GetHistoryByUserAsync(long userId, int months, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<FinancialScore>(
            @"SELECT * FROM [Analytics].[FinancialScore] 
              WHERE UserId = @UserId AND ScoreDate >= DATEADD(MONTH, -@Months, GETUTCDATE())
              ORDER BY ScoreDate ASC",
            new { UserId = userId, Months = months });
        return result.ToList();
    }

    public async Task<FinancialScore?> GetLatestByUserAsync(long userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<FinancialScore>(
            "SELECT TOP 1 * FROM [Analytics].[FinancialScore] WHERE UserId = @UserId ORDER BY ScoreDate DESC",
            new { UserId = userId });
    }

    public async Task<long> CalculateFinancialScoreAsync(long userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId);
        parameters.Add("@Id", dbType: System.Data.DbType.Int64, direction: System.Data.ParameterDirection.Output);

        await connection.ExecuteAsync(
            "Analytics.sp_CalculateFinancialScore", parameters,
            commandType: System.Data.CommandType.StoredProcedure);

        return parameters.Get<long>("@Id");
    }

    public async Task<FinancialScore> AddAsync(FinancialScore entity, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO [Analytics].[FinancialScore] (UserId, ScoreDate, OverallScore, ScoreGrade, SavingsRateScore, DebtToIncomeScore, EmergencyFundScore, InvestmentScore, GoalProgressScore, SavingsRatePct, DebtToIncomeRatio, EmergencyFundMonths, InvestmentToIncomeRatio, MonthlyIncome, MonthlyExpenses, MonthlySavings, TotalDebt, TotalInvestments, Recommendations, CreatedAt)
            VALUES (@UserId, @ScoreDate, @OverallScore, @ScoreGrade, @SavingsRateScore, @DebtToIncomeScore, @EmergencyFundScore, @InvestmentScore, @GoalProgressScore, @SavingsRatePct, @DebtToIncomeRatio, @EmergencyFundMonths, @InvestmentToIncomeRatio, @MonthlyIncome, @MonthlyExpenses, @MonthlySavings, @TotalDebt, @TotalInvestments, @Recommendations, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
        var id = await connection.ExecuteScalarAsync<long>(sql, entity);
        entity.Id = id;
        return entity;
    }

    public Task UpdateAsync(FinancialScore entity, CancellationToken ct = default) => Task.CompletedTask;

    public Task RemoveAsync(FinancialScore entity, CancellationToken ct = default) => Task.CompletedTask;
}
