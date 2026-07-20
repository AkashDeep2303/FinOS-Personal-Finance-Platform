using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.Investment.Domain.Entities;
using FinOS.Investment.Domain.Interfaces;

namespace FinOS.Investment.Infrastructure.Repositories;

public class SIPRepository : ISIPRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public SIPRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<SIP?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT s.*, h.* 
            FROM [Investment].[SIPs] s
            LEFT JOIN [Investment].[Holdings] h ON s.HoldingId = h.Id
            WHERE s.Id = @Id";

        var sipDict = new Dictionary<long, SIP>();
        var result = await connection.QueryAsync<SIP, Holding, SIP>(sql,
            (sip, holding) =>
            {
                if (!sipDict.TryGetValue(sip.Id, out var existingSip))
                {
                    existingSip = sip;
                    sipDict.Add(sip.Id, existingSip);
                }
                existingSip.Holding = holding;
                return existingSip;
            },
            new { Id = id }, splitOn: "Id");

        return sipDict.Values.FirstOrDefault();
    }

    public async Task<PagedResult<SIP>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $" WHERE {whereClause}";
        var countSql = $"SELECT COUNT(*) FROM [{schema}].[{tableName}]{where}";
        var dataSql = $"SELECT * FROM [{schema}].[{tableName}]{where} ORDER BY CreatedAt DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var dp = new DynamicParameters(param);
        dp.Add("@Offset", (query.PageNumber - 1) * query.PageSize);
        dp.Add("@PageSize", query.PageSize);

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param);
        var items = await connection.QueryAsync<SIP>(dataSql, dp);

        return new PagedResult<SIP>
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

    public async Task<List<SIP>> GetByUserIdAsync(long userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT s.*, h.* 
            FROM [Investment].[SIPs] s
            LEFT JOIN [Investment].[Holdings] h ON s.HoldingId = h.Id
            WHERE s.UserId = @UserId
            ORDER BY s.CreatedAt DESC";

        var sipDict = new Dictionary<long, SIP>();
        var result = await connection.QueryAsync<SIP, Holding, SIP>(sql,
            (sip, holding) =>
            {
                if (!sipDict.TryGetValue(sip.Id, out var existingSip))
                {
                    existingSip = sip;
                    sipDict.Add(sip.Id, existingSip);
                }
                existingSip.Holding = holding;
                return existingSip;
            },
            new { UserId = userId }, splitOn: "Id");

        return sipDict.Values.ToList();
    }

    public async Task<List<SIP>> GetActiveSIPsAsync(CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT s.*, h.* 
            FROM [Investment].[SIPs] s
            LEFT JOIN [Investment].[Holdings] h ON s.HoldingId = h.Id
            WHERE s.IsActive = 1";

        var sipDict = new Dictionary<long, SIP>();
        var result = await connection.QueryAsync<SIP, Holding, SIP>(sql,
            (sip, holding) =>
            {
                if (!sipDict.TryGetValue(sip.Id, out var existingSip))
                {
                    existingSip = sip;
                    sipDict.Add(sip.Id, existingSip);
                }
                existingSip.Holding = holding;
                return existingSip;
            },
            splitOn: "Id");

        return sipDict.Values.ToList();
    }

    public async Task<List<SIP>> GetDueSIPsAsync(DateTime asOfDate, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT s.*, h.* 
            FROM [Investment].[SIPs] s
            LEFT JOIN [Investment].[Holdings] h ON s.HoldingId = h.Id
            WHERE s.IsActive = 1 AND (s.NextExecutionDate IS NULL OR s.NextExecutionDate <= @AsOfDate)";

        var sipDict = new Dictionary<long, SIP>();
        var result = await connection.QueryAsync<SIP, Holding, SIP>(sql,
            (sip, holding) =>
            {
                if (!sipDict.TryGetValue(sip.Id, out var existingSip))
                {
                    existingSip = sip;
                    sipDict.Add(sip.Id, existingSip);
                }
                existingSip.Holding = holding;
                return existingSip;
            },
            new { AsOfDate = asOfDate }, splitOn: "Id");

        return sipDict.Values.ToList();
    }

    public async Task<long> CreateSIPAsync(long userId, long? holdingId, string sipName, decimal amount, string frequency, DateTime startDate, DateTime? endDate, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId);
        parameters.Add("@HoldingId", holdingId);
        parameters.Add("@SIPName", sipName);
        parameters.Add("@Amount", amount);
        parameters.Add("@Frequency", frequency);
        parameters.Add("@StartDate", startDate);
        parameters.Add("@EndDate", endDate);
        parameters.Add("@Id", dbType: System.Data.DbType.Int64, direction: System.Data.ParameterDirection.Output);

        await connection.ExecuteAsync(
            "Investment.sp_CreateSIP", parameters,
            commandType: System.Data.CommandType.StoredProcedure);

        return parameters.Get<long>("@Id");
    }

    public async Task ProcessSIPInstallmentAsync(long sipId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "Investment.sp_ProcessSIPInstallment",
            new { SIPId = sipId },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    public async Task<SIP> AddAsync(SIP entity, CancellationToken ct = default)
    {
        var id = await CreateSIPAsync(entity.UserId, entity.HoldingId, entity.Name, entity.Amount, entity.Frequency.ToString(), entity.StartDate, entity.EndDate, ct);
        entity.Id = id;
        return entity;
    }

    public Task UpdateAsync(SIP entity, CancellationToken ct = default) => Task.CompletedTask;

    public Task RemoveAsync(SIP entity, CancellationToken ct = default) => Task.CompletedTask;
}
