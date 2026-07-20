using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.Analytics.Domain.Entities;
using FinOS.Analytics.Domain.Interfaces;

namespace FinOS.Analytics.Infrastructure.Repositories;

public class NetWorthRepository : INetWorthRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public NetWorthRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<NetWorthSnapshot?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<NetWorthSnapshot>(
            "SELECT * FROM [Analytics].[NetWorthSnapshots] WHERE Id = @Id", new { Id = id });
    }

    public async Task<PagedResult<NetWorthSnapshot>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $" WHERE {whereClause}";
        var countSql = $"SELECT COUNT(*) FROM [{schema}].[{tableName}]{where}";
        var dataSql = $"SELECT * FROM [{schema}].[{tableName}]{where} ORDER BY SnapshotDate DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var dp = new DynamicParameters(param);
        dp.Add("@Offset", (query.PageNumber - 1) * query.PageSize);
        dp.Add("@PageSize", query.PageSize);

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param);
        var items = await connection.QueryAsync<NetWorthSnapshot>(dataSql, dp);

        return new PagedResult<NetWorthSnapshot>
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

    public async Task<List<NetWorthSnapshot>> GetByUserAsync(long userId, int months, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<NetWorthSnapshot>(
            @"SELECT * FROM [Analytics].[NetWorthSnapshots] 
              WHERE UserId = @UserId AND SnapshotDate >= DATEADD(MONTH, -@Months, GETUTCDATE())
              ORDER BY SnapshotDate ASC",
            new { UserId = userId, Months = months });
        return result.ToList();
    }

    public async Task<NetWorthSnapshot?> GetLatestByUserAsync(long userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<NetWorthSnapshot>(
            "SELECT TOP 1 * FROM [Analytics].[NetWorthSnapshots] WHERE UserId = @UserId ORDER BY SnapshotDate DESC",
            new { UserId = userId });
    }

    public async Task<long> CalculateNetWorthAsync(long userId, DateTime snapshotDate, decimal totalAssets, decimal totalLiabilities, decimal netWorth, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId);
        parameters.Add("@SnapshotDate", snapshotDate);
        parameters.Add("@TotalAssets", totalAssets);
        parameters.Add("@TotalLiabilities", totalLiabilities);
        parameters.Add("@NetWorth", netWorth);
        parameters.Add("@Id", dbType: System.Data.DbType.Int64, direction: System.Data.ParameterDirection.Output);

        await connection.ExecuteAsync(
            "Analytics.sp_CalculateNetWorth", parameters,
            commandType: System.Data.CommandType.StoredProcedure);

        return parameters.Get<long>("@Id");
    }

    public async Task<NetWorthSnapshot> AddAsync(NetWorthSnapshot entity, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO [Analytics].[NetWorthSnapshots] (UserId, SnapshotDate, TotalAssets, TotalLiabilities, NetWorth, CashAndBank, InvestmentValue, RealEstateValue, GoldValue, OtherAssets, LoanOutstanding, CreditCardOutstanding, OtherLiabilities, ChangeFromPrevious, ChangePctFromPrevious, CreatedAt)
            VALUES (@UserId, @SnapshotDate, @TotalAssets, @TotalLiabilities, @NetWorth, @CashAndBank, @InvestmentValue, @RealEstateValue, @GoldValue, @OtherAssets, @LoanOutstanding, @CreditCardOutstanding, @OtherLiabilities, @ChangeFromPrevious, @ChangePctFromPrevious, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
        var id = await connection.ExecuteScalarAsync<long>(sql, entity);
        entity.Id = id;
        return entity;
    }

    public Task UpdateAsync(NetWorthSnapshot entity, CancellationToken ct = default) => Task.CompletedTask;

    public Task RemoveAsync(NetWorthSnapshot entity, CancellationToken ct = default) => Task.CompletedTask;
}
