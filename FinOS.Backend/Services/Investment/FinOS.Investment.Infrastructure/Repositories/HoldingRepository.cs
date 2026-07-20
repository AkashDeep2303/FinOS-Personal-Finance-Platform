using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.Investment.Domain.Entities;
using FinOS.Investment.Domain.Interfaces;

namespace FinOS.Investment.Infrastructure.Repositories;

public class HoldingRepository : IHoldingRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public HoldingRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Holding?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Holding>(
            "SELECT * FROM [Investment].[Holdings] WHERE Id = @Id", new { Id = id });
    }

    public async Task<PagedResult<Holding>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $" WHERE {whereClause}";
        var countSql = $"SELECT COUNT(*) FROM [{schema}].[{tableName}]{where}";
        var dataSql = $"SELECT * FROM [{schema}].[{tableName}]{where} ORDER BY CreatedAt DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var dp = new DynamicParameters(param);
        dp.Add("@Offset", (query.PageNumber - 1) * query.PageSize);
        dp.Add("@PageSize", query.PageSize);

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param);
        var items = await connection.QueryAsync<Holding>(dataSql, dp);

        return new PagedResult<Holding>
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

    public async Task<List<Holding>> GetByPortfolioIdAsync(long portfolioId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<Holding>(
            "SELECT * FROM [Investment].[Holdings] WHERE PortfolioId = @PortfolioId",
            new { PortfolioId = portfolioId });
        return result.ToList();
    }

    public async Task<Holding?> GetWithTransactionsAsync(long holdingId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT * FROM [Investment].[Holdings] WHERE Id = @HoldingId;
            SELECT * FROM [Investment].[InvestmentTransactions] WHERE HoldingId = @HoldingId ORDER BY TransactionDate DESC;";

        using var multi = await connection.QueryMultipleAsync(sql, new { HoldingId = holdingId });
        var holding = await multi.ReadFirstOrDefaultAsync<Holding>();
        if (holding != null)
        {
            var transactions = (await multi.ReadAsync<InvestmentTransaction>()).ToList();
            holding.InvestmentTransactions = transactions;
        }
        return holding;
    }

    public async Task<List<Holding>> GetActiveByPortfolioIdAsync(long portfolioId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<Holding>(
            "SELECT * FROM [Investment].[Holdings] WHERE PortfolioId = @PortfolioId AND IsActive = 1",
            new { PortfolioId = portfolioId });
        return result.ToList();
    }

    public async Task<long> AddHoldingAsync(long portfolioId, string assetClass, string name, string? ticker, decimal quantity, decimal avgBuyPrice, decimal currentPrice, string? fundCategory, string? riskLevel, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@PortfolioId", portfolioId);
        parameters.Add("@AssetClass", assetClass);
        parameters.Add("@Name", name);
        parameters.Add("@Ticker", ticker);
        parameters.Add("@Quantity", quantity);
        parameters.Add("@AvgBuyPrice", avgBuyPrice);
        parameters.Add("@CurrentPrice", currentPrice);
        parameters.Add("@FundCategory", fundCategory);
        parameters.Add("@RiskLevel", riskLevel);
        parameters.Add("@Id", dbType: System.Data.DbType.Int64, direction: System.Data.ParameterDirection.Output);

        await connection.ExecuteAsync(
            "Investment.sp_AddHolding", parameters,
            commandType: System.Data.CommandType.StoredProcedure);

        return parameters.Get<long>("@Id");
    }

    public async Task UpdateHoldingPriceAsync(long holdingId, decimal currentPrice, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "Investment.sp_UpdateHoldingPrice",
            new { HoldingId = holdingId, CurrentPrice = currentPrice },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    public async Task<Holding> AddAsync(Holding entity, CancellationToken ct = default)
    {
        var id = await AddHoldingAsync(entity.PortfolioId, entity.FundCategory?.ToString() ?? "Equity", entity.Name, entity.Symbol, entity.Quantity, entity.AvgPurchasePrice, entity.CurrentPrice, entity.FundCategory?.ToString(), entity.RiskLevel?.ToString(), ct);
        entity.Id = id;
        return entity;
    }

    public Task UpdateAsync(Holding entity, CancellationToken ct = default) => Task.CompletedTask;

    public Task RemoveAsync(Holding entity, CancellationToken ct = default) => Task.CompletedTask;
}
