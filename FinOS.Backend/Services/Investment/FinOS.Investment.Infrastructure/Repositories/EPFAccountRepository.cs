using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.Investment.Domain.Entities;
using FinOS.Investment.Domain.Interfaces;

namespace FinOS.Investment.Infrastructure.Repositories;

public class EPFAccountRepository : IEPFAccountRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public EPFAccountRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<EPFAccount?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<EPFAccount>(
            "SELECT * FROM [Investment].[EPFAccounts] WHERE Id = @Id", new { Id = id });
    }

    public async Task<PagedResult<EPFAccount>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $" WHERE {whereClause}";
        var countSql = $"SELECT COUNT(*) FROM [{schema}].[{tableName}]{where}";
        var dataSql = $"SELECT * FROM [{schema}].[{tableName}]{where} ORDER BY CreatedAt DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var dp = new DynamicParameters(param);
        dp.Add("@Offset", (query.PageNumber - 1) * query.PageSize);
        dp.Add("@PageSize", query.PageSize);

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param);
        var items = await connection.QueryAsync<EPFAccount>(dataSql, dp);

        return new PagedResult<EPFAccount>
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

    public async Task<List<EPFAccount>> GetByUserIdAsync(long userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT e.*, c.* 
            FROM [Investment].[EPFAccounts] e
            LEFT JOIN [Investment].[EPFContributions] c ON e.Id = c.EPFAccountId
            WHERE e.UserId = @UserId";

        var accountDict = new Dictionary<long, EPFAccount>();
        var result = await connection.QueryAsync<EPFAccount, EPFContribution, EPFAccount>(sql,
            (account, contribution) =>
            {
                if (!accountDict.TryGetValue(account.Id, out var existingAccount))
                {
                    existingAccount = account;
                    existingAccount.Contributions = new List<EPFContribution>();
                    accountDict.Add(account.Id, existingAccount);
                }
                if (contribution != null)
                {
                    existingAccount.Contributions.Add(contribution);
                }
                return existingAccount;
            },
            new { UserId = userId }, splitOn: "Id");

        return accountDict.Values.ToList();
    }

    public async Task<EPFAccount?> GetWithContributionsAsync(long epfAccountId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT e.*, c.* 
            FROM [Investment].[EPFAccounts] e
            LEFT JOIN [Investment].[EPFContributions] c ON e.Id = c.EPFAccountId
            WHERE e.Id = @EpfAccountId";

        var accountDict = new Dictionary<long, EPFAccount>();
        var result = await connection.QueryAsync<EPFAccount, EPFContribution, EPFAccount>(sql,
            (account, contribution) =>
            {
                if (!accountDict.TryGetValue(account.Id, out var existingAccount))
                {
                    existingAccount = account;
                    existingAccount.Contributions = new List<EPFContribution>();
                    accountDict.Add(account.Id, existingAccount);
                }
                if (contribution != null)
                {
                    existingAccount.Contributions.Add(contribution);
                }
                return existingAccount;
            },
            new { EpfAccountId = epfAccountId }, splitOn: "Id");

        return accountDict.Values.FirstOrDefault();
    }

    public async Task UpdateEPFContributionAsync(long epfAccountId, decimal employeeContribution, decimal employerContribution, DateTime contributionDate, string? financialYear, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "Investment.sp_UpdateEPFContribution",
            new
            {
                EPFAccountId = epfAccountId,
                EmployeeContribution = employeeContribution,
                EmployerContribution = employerContribution,
                ContributionDate = contributionDate,
                FinancialYear = financialYear
            },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    public Task<EPFAccount> AddAsync(EPFAccount entity, CancellationToken ct = default)
    {
        throw new NotImplementedException("EPF accounts are created via stored procedure. Use UpdateEPFContributionAsync instead.");
    }

    public Task UpdateAsync(EPFAccount entity, CancellationToken ct = default) => Task.CompletedTask;

    public Task RemoveAsync(EPFAccount entity, CancellationToken ct = default) => Task.CompletedTask;
}
