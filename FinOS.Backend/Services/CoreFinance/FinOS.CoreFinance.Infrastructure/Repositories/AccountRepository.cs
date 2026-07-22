using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.CoreFinance.Domain.Entities;
using FinOS.CoreFinance.Domain.Interfaces;
using Microsoft.Data.SqlClient;

namespace FinOS.CoreFinance.Infrastructure.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public AccountRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Account?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT a.*, at.*
            FROM Core.Accounts a
            LEFT JOIN Core.AccountTypes at ON a.AccountTypeId = at.Id
            WHERE a.Id = @Id AND a.DeletedAt IS NULL";
        var result = await connection.QueryAsync<Account, AccountType, Account>(
            sql,
            (account, accountType) =>
            {
                account.AccountType = accountType;
                return account;
            },
            new { Id = id },
            commandTimeout: 30);
        return result.FirstOrDefault();
    }

    public async Task<PagedResult<Account>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $" AND {whereClause}";
        var countSql = $"SELECT COUNT(*) FROM [{schema}].[{tableName}] WHERE DeletedAt IS NULL{where}";
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param);

        var offset = (query.PageNumber - 1) * query.PageSize;
        var dataSql = $"SELECT * FROM [{schema}].[{tableName}] WHERE DeletedAt IS NULL{where} ORDER BY Name OFFSET {offset} ROWS FETCH NEXT {query.PageSize} ROWS ONLY";
        var items = (await connection.QueryAsync<Account>(dataSql, param)).ToList();

        return new PagedResult<Account>
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    public async Task<long> CountAsync(string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $" AND {whereClause}";
        var sql = $"SELECT COUNT(*) FROM [{schema}].[{tableName}] WHERE DeletedAt IS NULL{where}";
        return await connection.ExecuteScalarAsync<long>(sql, param);
    }

    public async Task<List<Account>> GetByUserIdAsync(long userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT a.*, at.*
            FROM Core.Accounts a
            LEFT JOIN Core.AccountTypes at ON a.AccountTypeId = at.Id
            WHERE a.UserId = @UserId AND a.DeletedAt IS NULL
            ORDER BY a.Name";
        var result = await connection.QueryAsync<Account, AccountType, Account>(
            sql,
            (account, accountType) =>
            {
                account.AccountType = accountType;
                return account;
            },
            new { UserId = userId });
        return result.ToList();
    }

    public async Task<List<Account>> GetActiveByUserIdAsync(long userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT a.*, at.*
            FROM Core.Accounts a
            LEFT JOIN Core.AccountTypes at ON a.AccountTypeId = at.Id
            WHERE a.UserId = @UserId AND a.DeletedAt IS NULL
            ORDER BY a.Name";
        var result = await connection.QueryAsync<Account, AccountType, Account>(
            sql,
            (account, accountType) =>
            {
                account.AccountType = accountType;
                return account;
            },
            new { UserId = userId });
        return result.ToList();
    }

    public async Task<bool> ExistsAsync(long userId, long accountId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = "SELECT COUNT(1) FROM Core.Accounts WHERE UserId = @UserId AND Id = @AccountId AND DeletedAt IS NULL";
        return await connection.ExecuteScalarAsync<bool>(sql, new { UserId = userId, AccountId = accountId });
    }

    public async Task<long> CreateAsync(Account account, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = "Core.sp_CreateAccount";
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", account.UserId);
        parameters.Add("@AccountTypeId", account.AccountTypeId);
        parameters.Add("@Name", account.Name);
        parameters.Add("@InstitutionName", account.InstitutionName);
        parameters.Add("@AccountNumber", account.AccountNumber);
        parameters.Add("@Balance", account.Balance);
        parameters.Add("@CreditLimit", account.CreditLimit);
        parameters.Add("@Currency", account.Currency);
        parameters.Add("@Color", account.Color);
        parameters.Add("@Icon", account.Icon);
        parameters.Add("@IsIncludedInNetWorth", account.IsIncludedInNetWorth);
        parameters.Add("@Notes", account.Notes);
        parameters.Add("@NewAccountId", dbType: System.Data.DbType.Int64, direction: System.Data.ParameterDirection.Output);

        await connection.ExecuteAsync(sql, parameters, commandType: System.Data.CommandType.StoredProcedure);
        return parameters.Get<long>("@NewAccountId");
    }

    public async Task UpdateAsync(Account account, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            UPDATE Core.Accounts SET
                AccountTypeId = @AccountTypeId,
                Name = @Name,
                InstitutionName = @InstitutionName,
                AccountNumber = @AccountNumber,
                Balance = @Balance,
                CreditLimit = @CreditLimit,
                Currency = @Currency,
                Color = @Color,
                Icon = @Icon,
                IsIncludedInNetWorth = @IsIncludedInNetWorth,
                IsSynced = @IsSynced,
                SyncProvider = @SyncProvider,
                SyncAccountId = @SyncAccountId,
                LastSyncedAt = @LastSyncedAt,
                Notes = @Notes,
                IsActive = @IsActive,
                DeletedAt = @DeletedAt,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id AND DeletedAt IS NULL";
        await connection.ExecuteAsync(sql, account);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = "UPDATE Core.Accounts SET DeletedAt = @DeletedAt, IsActive = 0, UpdatedAt = @UpdatedAt WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id, DeletedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
    }

    public async Task UpdateBalanceAsync(long accountId, decimal newBalance, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "Core.sp_UpdateAccountBalance",
            new { AccountId = accountId, NewBalance = newBalance },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    public async Task<Account> AddAsync(Account entity, CancellationToken ct = default)
    {
        var id = await CreateAsync(entity, ct);
        entity.Id = id;
        return entity;
    }


    public Task RemoveAsync(Account entity, CancellationToken ct = default) => Task.CompletedTask;
}
