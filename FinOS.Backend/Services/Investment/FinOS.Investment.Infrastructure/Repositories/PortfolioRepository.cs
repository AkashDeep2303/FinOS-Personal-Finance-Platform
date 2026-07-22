using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.Investment.Domain.Entities;
using FinOS.Investment.Domain.Interfaces;

namespace FinOS.Investment.Infrastructure.Repositories;

public class PortfolioRepository : IPortfolioRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public PortfolioRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Portfolio?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Portfolio>(
            "SELECT * FROM [Investment].[Portfolios] WHERE Id = @Id", new { Id = id });
    }

    public async Task<PagedResult<Portfolio>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $" WHERE {whereClause}";
        var countSql = $"SELECT COUNT(*) FROM [{schema}].[{tableName}]{where}";
        var dataSql = $"SELECT * FROM [{schema}].[{tableName}]{where} ORDER BY CreatedAt DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var dp = new DynamicParameters(param);
        dp.Add("@Offset", (query.PageNumber - 1) * query.PageSize);
        dp.Add("@PageSize", query.PageSize);

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param);
        var items = await connection.QueryAsync<Portfolio>(dataSql, dp);

        return new PagedResult<Portfolio>
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
        var sql = $"SELECT COUNT(*) FROM [{schema}].[{tableName}]{where}";
        return await connection.ExecuteScalarAsync<long>(sql, param);
    }

    public async Task<List<Portfolio>> GetByUserIdAsync(long userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var portfolios = await connection.QueryAsync<Portfolio>(
            "SELECT * FROM [Investment].[Portfolios] WHERE UserId = @UserId ORDER BY CreatedAt DESC",
            new { UserId = userId });
        return portfolios.ToList();
    }

    public async Task<Portfolio?> GetWithHoldingsAsync(long portfolioId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT * FROM [Investment].[Portfolios] WHERE Id = @PortfolioId;
            SELECT * FROM [Investment].[Holdings] WHERE PortfolioId = @PortfolioId;";

        using var multi = await connection.QueryMultipleAsync(sql, new { PortfolioId = portfolioId });
        var portfolio = await multi.ReadFirstOrDefaultAsync<Portfolio>();
        if (portfolio != null)
        {
            var holdings = (await multi.ReadAsync<Holding>()).ToList();
            portfolio.Holdings = holdings;
        }
        return portfolio;
    }

    public async Task<long> CreatePortfolioAsync(long userId, string name, string? description, string currency, bool isDefault = false, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO [Investment].[Portfolios]
                (UserId, Name, Description, Currency, IsDefault)
            OUTPUT INSERTED.Id
            VALUES (@UserId, @Name, @Description, @Currency, @IsDefault);";

        return await connection.ExecuteScalarAsync<long>(sql, new
        {
            UserId = userId,
            Name = name,
            Description = description,
            Currency = string.IsNullOrWhiteSpace(currency) ? "INR" : currency,
            IsDefault = isDefault
        });
    }

    public async Task<PortfolioSummaryResult?> GetPortfolioSummaryAsync(long portfolioId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<PortfolioSummaryResult>(
            "Investment.sp_GetPortfolioSummary", new { PortfolioId = portfolioId },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    public async Task RecordInvestmentTransactionAsync(long holdingId, string transactionType, decimal quantity, decimal pricePerUnit, decimal totalAmount, DateTime transactionDate, string? notes, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "Investment.sp_RecordInvestmentTransaction",
            new
            {
                HoldingId = holdingId,
                TransactionType = transactionType,
                Quantity = quantity,
                PricePerUnit = pricePerUnit,
                TotalAmount = totalAmount,
                TransactionDate = transactionDate,
                Notes = notes
            },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    public async Task<Portfolio> AddAsync(Portfolio entity, CancellationToken ct = default)
    {
        var id = await CreatePortfolioAsync(entity.UserId, entity.Name, entity.Description, entity.Currency, entity.IsDefault, ct);
        entity.Id = id;
        return entity;
    }

    public Task UpdateAsync(Portfolio entity, CancellationToken ct = default) => Task.CompletedTask;

    public Task RemoveAsync(Portfolio entity, CancellationToken ct = default) => Task.CompletedTask;
}

public class PortfolioSummaryResult
{
    public long PortfolioId { get; set; }
    public string? PortfolioName { get; set; }
    public decimal TotalInvested { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal TotalReturns { get; set; }
    public decimal ReturnPercentage { get; set; }
}
