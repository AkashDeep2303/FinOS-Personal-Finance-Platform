using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.Investment.Domain.Entities;
using FinOS.Investment.Domain.Enums;
using FinOS.Investment.Domain.Interfaces;

namespace FinOS.Investment.Infrastructure.Repositories;

public class GoldPriceRepository : IGoldPriceRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public GoldPriceRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<GoldPriceHistory?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<GoldPriceHistory>(
            "SELECT * FROM [Investment].[GoldPriceHistory] WHERE Id = @Id", new { Id = id });
    }

    public async Task<PagedResult<GoldPriceHistory>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $" WHERE {whereClause}";
        var countSql = $"SELECT COUNT(*) FROM [{schema}].[{tableName}]{where}";
        var dataSql = $"SELECT * FROM [{schema}].[{tableName}]{where} ORDER BY PriceDate DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var dp = new DynamicParameters(param);
        dp.Add("@Offset", (query.PageNumber - 1) * query.PageSize);
        dp.Add("@PageSize", query.PageSize);

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param);
        var items = await connection.QueryAsync<GoldPriceHistory>(dataSql, dp);

        return new PagedResult<GoldPriceHistory>
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

    public async Task<GoldPriceHistory?> GetLatestPriceAsync(GoldType goldType, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<GoldPriceHistory>(
            "SELECT TOP 1 * FROM [Investment].[GoldPriceHistory] WHERE GoldType = @GoldType ORDER BY PriceDate DESC",
            new { GoldType = goldType.ToString() });
    }

    public async Task<List<GoldPriceHistory>> GetPriceHistoryAsync(GoldType goldType, DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<GoldPriceHistory>(
            "SELECT * FROM [Investment].[GoldPriceHistory] WHERE GoldType = @GoldType AND PriceDate >= @From AND PriceDate <= @To ORDER BY PriceDate",
            new { GoldType = goldType.ToString(), From = from, To = to });
        return result.ToList();
    }

    public Task<GoldPriceHistory> AddAsync(GoldPriceHistory entity, CancellationToken ct = default)
    {
        throw new NotImplementedException("Gold prices are populated via external data feed.");
    }

    public Task UpdateAsync(GoldPriceHistory entity, CancellationToken ct = default) => Task.CompletedTask;

    public Task RemoveAsync(GoldPriceHistory entity, CancellationToken ct = default) => Task.CompletedTask;
}
