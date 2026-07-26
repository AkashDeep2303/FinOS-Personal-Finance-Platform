using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.CoreFinance.Domain.Entities;
using FinOS.CoreFinance.Domain.Enums;
using FinOS.CoreFinance.Domain.Interfaces;
using Microsoft.Data.SqlClient;

namespace FinOS.CoreFinance.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public TransactionRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Transaction?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT t.*, a.*, c.*, ta.*, tg.*
            FROM Core.Transactions t
            LEFT JOIN Core.Accounts a ON t.AccountId = a.Id
            LEFT JOIN Core.Categories c ON t.CategoryId = c.Id
            LEFT JOIN Core.TransactionTags ta ON t.Id = ta.TransactionId
            LEFT JOIN Core.Tags tg ON ta.TagId = tg.Id
            WHERE t.Id = @Id AND t.DeletedAt IS NULL";

        var transactionDict = new Dictionary<long, Transaction>();

        await connection.QueryAsync<Transaction, Account, Category, TransactionTag, Tag, Transaction>(
            sql,
            (transaction, account, category, transactionTag, tag) =>
            {
                if (!transactionDict.TryGetValue(transaction.Id, out var existingTransaction))
                {
                    existingTransaction = transaction;
                    existingTransaction.Account = account;
                    existingTransaction.Category = category;
                    existingTransaction.Tags = new List<TransactionTag>();
                    transactionDict.Add(existingTransaction.Id, existingTransaction);
                }

                if (transactionTag != null && tag != null)
                {
                    transactionTag.Tag = tag;
                    ((List<TransactionTag>)existingTransaction.Tags).Add(transactionTag);
                }

                return existingTransaction;
            },
            new { Id = id },
            splitOn: "Id,Id,TransactionId,Id");

        return transactionDict.Values.FirstOrDefault();
    }

    public async Task<PagedResult<Transaction>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $" AND {whereClause}";
        var countSql = $"SELECT COUNT(*) FROM [{schema}].[{tableName}] WHERE DeletedAt IS NULL{where}";
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param);

        var offset = (query.PageNumber - 1) * query.PageSize;
        var dataSql = $"SELECT * FROM [{schema}].[{tableName}] WHERE DeletedAt IS NULL{where} ORDER BY TransactionDate DESC OFFSET {offset} ROWS FETCH NEXT {query.PageSize} ROWS ONLY";
        var items = (await connection.QueryAsync<Transaction>(dataSql, param)).ToList();

        return new PagedResult<Transaction>
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

    public async Task<PagedResult<Transaction>> GetByDateRangeAsync(
        long userId, DateTime startDate, DateTime endDate,
        PagedQuery query, TransactionType? type = null,
        long? accountId = null, long? categoryId = null,
        string? merchantName = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId);
        parameters.Add("@StartDate", startDate);
        parameters.Add("@EndDate", endDate);
        parameters.Add("@PageNumber", query.PageNumber);
        parameters.Add("@PageSize", query.PageSize);
        parameters.Add("@Type", type?.ToString());
        parameters.Add("@AccountId", accountId);
        parameters.Add("@CategoryId", categoryId);
        parameters.Add("@SearchTerm", query.SearchTerm);

        using var multi = await connection.QueryMultipleAsync(
            "Core.sp_GetTransactionsByDateRange",
            parameters,
            commandType: System.Data.CommandType.StoredProcedure);

        var items = (await multi.ReadAsync<Transaction>()).ToList();
        var totalCount = await multi.ReadFirstAsync<int>();

        return new PagedResult<Transaction>
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    public async Task<MonthlySummaryData> GetMonthlySummaryAsync(long userId, int year, int month, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId);
        parameters.Add("@Year", year);
        parameters.Add("@Month", month);

        using var multi = await connection.QueryMultipleAsync(
            "Core.sp_GetMonthlySummary",
            parameters,
            commandType: System.Data.CommandType.StoredProcedure);

        var summary = await multi.ReadFirstOrDefaultAsync<MonthlySummaryData>();
        var categorySummaries = (await multi.ReadAsync<CategorySummaryData>()).ToList();

        if (summary != null)
        {
            summary.CategorySummaries = categorySummaries;
        }

        return summary ?? new MonthlySummaryData();
    }

    public async Task<List<Transaction>> GetByMerchantNameAsync(long userId, string merchantName, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT t.*, a.Name as AccountName
            FROM Core.Transactions t
            LEFT JOIN Core.Accounts a ON t.AccountId = a.Id
            WHERE t.UserId = @UserId AND t.MerchantName = @MerchantName AND t.DeletedAt IS NULL
            ORDER BY t.TransactionDate DESC";
        var result = await connection.QueryAsync<Transaction>(sql, new { UserId = userId, MerchantName = merchantName });
        return result.ToList();
    }

    public async Task<long> CreateAsync(Transaction transaction, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = "Core.sp_CreateTransaction";
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", transaction.UserId);
        parameters.Add("@AccountId", transaction.AccountId);
        parameters.Add("@CategoryId", transaction.CategoryId);
        parameters.Add("@TransferAccountId", transaction.TransferAccountId);
        parameters.Add("@Type", transaction.Type.ToString());
        parameters.Add("@Amount", transaction.Amount);
        parameters.Add("@Currency", transaction.Currency);
        parameters.Add("@ExchangeRate", transaction.ExchangeRate);
        parameters.Add("@OriginalAmount", transaction.OriginalAmount);
        parameters.Add("@OriginalCurrency", transaction.OriginalCurrency);
        parameters.Add("@Description", transaction.Description);
        parameters.Add("@Notes", transaction.Notes);
        parameters.Add("@TransactionDate", transaction.TransactionDate);
        parameters.Add("@TransactionTime", transaction.TransactionTime);
        parameters.Add("@ValueDate", transaction.ValueDate);
        parameters.Add("@ReferenceNumber", transaction.ReferenceNumber);
        parameters.Add("@MerchantName", transaction.MerchantName);
        parameters.Add("@MerchantCategory", transaction.MerchantCategory);
        parameters.Add("@IsRecurring", transaction.IsRecurring);
        parameters.Add("@RecurringScheduleId", transaction.RecurringScheduleId);
        parameters.Add("@IsFlagged", transaction.IsFlagged);
        parameters.Add("@AttachmentUrls", transaction.AttachmentUrls);
        parameters.Add("@LocationLat", transaction.LocationLat);
        parameters.Add("@LocationLng", transaction.LocationLng);
        parameters.Add("@LocationName", transaction.LocationName);
        parameters.Add("@Source", transaction.Source.ToString());
        parameters.Add("@ImportBatchId", transaction.ImportBatchId);
        parameters.Add("@NewTransactionId", dbType: System.Data.DbType.Int64, direction: System.Data.ParameterDirection.Output);

        await connection.ExecuteAsync(sql, parameters, commandType: System.Data.CommandType.StoredProcedure);
        return parameters.Get<long>("@NewTransactionId");
    }

    public async Task UpdateAsync(Transaction transaction, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@TransactionId", transaction.Id);
        parameters.Add("@UserId", transaction.UserId);
        parameters.Add("@AccountId", transaction.AccountId);
        parameters.Add("@CategoryId", transaction.CategoryId);
        parameters.Add("@TransferAccountId", transaction.TransferAccountId);
        parameters.Add("@Type", transaction.Type.ToString());
        parameters.Add("@Amount", transaction.Amount);
        parameters.Add("@Description", transaction.Description);
        parameters.Add("@Notes", transaction.Notes);
        parameters.Add("@TransactionDate", transaction.TransactionDate);
        parameters.Add("@MerchantName", transaction.MerchantName);
        parameters.Add("@MerchantCategory", transaction.MerchantCategory);
        parameters.Add("@IsFlagged", transaction.IsFlagged);
        parameters.Add("@ReferenceNumber", transaction.ReferenceNumber);

        await connection.ExecuteAsync(
            "Core.sp_UpdateTransaction",
            parameters,
            commandType: System.Data.CommandType.StoredProcedure);
    }
    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "Core.sp_DeleteTransaction",
            new { TransactionId = id },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    public async Task<List<Transaction>> SplitAsync(long transactionId, List<(decimal Amount, string? Notes)> splits, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var splitAmounts = string.Join(",", splits.Select(s => s.Amount.ToString()));
        var splitNotes = string.Join(",", splits.Select(s => s.Notes ?? ""));

        var parameters = new DynamicParameters();
        parameters.Add("@TransactionId", transactionId);
        parameters.Add("@SplitAmounts", splitAmounts);
        parameters.Add("@SplitNotes", splitNotes);

        var result = await connection.QueryAsync<Transaction>(
            "Core.sp_SplitTransaction",
            parameters,
            commandType: System.Data.CommandType.StoredProcedure);

        return result.ToList();
    }

    public async Task<Transaction> AddAsync(Transaction entity, CancellationToken ct = default)
    {
        var id = await CreateAsync(entity, ct);
        entity.Id = id;
        return entity;
    }


    public Task RemoveAsync(Transaction entity, CancellationToken ct = default) => Task.CompletedTask;
}
