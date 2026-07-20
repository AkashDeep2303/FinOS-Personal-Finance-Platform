using System.Data;
using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.Budget.Domain.Entities;
using FinOS.Budget.Domain.Interfaces;

namespace FinOS.Budget.Infrastructure.Repositories;

public class BudgetCategoryRepository : IBudgetCategoryRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public BudgetCategoryRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<BudgetCategory?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var categoryDict = new Dictionary<long, BudgetCategory>();
        var sql = @"
            SELECT bc.*, ba.*
            FROM Budget.BudgetCategories bc
            LEFT JOIN Budget.BudgetAlerts ba ON bc.Id = ba.BudgetCategoryId
            WHERE bc.Id = @Id";

        await connection.QueryAsync<BudgetCategory, BudgetAlert, BudgetCategory>(
            sql,
            (category, alert) =>
            {
                if (!categoryDict.TryGetValue(category.Id, out var existing))
                {
                    existing = category;
                    existing.Alerts = new List<BudgetAlert>();
                    categoryDict.Add(existing.Id, existing);
                }
                if (alert != null) ((List<BudgetAlert>)existing.Alerts).Add(alert);
                return existing;
            },
            new { Id = id },
            splitOn: "Id");

        return categoryDict.Values.FirstOrDefault();
    }

    public async Task<PagedResult<BudgetCategory>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause)
            ? ""
            : $"WHERE {whereClause}";
        var sortDirection = query.SortDirection?.ToLower() == "asc" ? "ASC" : "DESC";
        var sortColumn = !string.IsNullOrWhiteSpace(query.SortBy) ? query.SortBy : "SortOrder";
        var offset = (query.PageNumber - 1) * query.PageSize;

        var countSql = $"SELECT COUNT(1) FROM [{schema}].[{tableName}] {where}";
        var dataSql = $"""
            SELECT * FROM [{schema}].[{tableName}] {where}
            ORDER BY [{sortColumn}] {sortDirection}
            OFFSET {offset} ROWS FETCH NEXT {query.PageSize} ROWS ONLY
            """;

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param);
        var items = (await connection.QueryAsync<BudgetCategory>(dataSql, param)).ToList();

        return new PagedResult<BudgetCategory>
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
        var where = string.IsNullOrWhiteSpace(whereClause)
            ? ""
            : $"WHERE {whereClause}";
        return await connection.ExecuteScalarAsync<long>(
            $"SELECT COUNT(1) FROM [{schema}].[{tableName}] {where}", param);
    }

    public async Task<List<BudgetCategory>> GetByBudgetIdAsync(long budgetId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<BudgetCategory>(
            "SELECT * FROM Budget.BudgetCategories WHERE BudgetId = @BudgetId ORDER BY SortOrder",
            new { BudgetId = budgetId });
        return result.ToList();
    }

    public async Task<List<BudgetCategory>> GetByCategoryIdAsync(long categoryId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<BudgetCategory>(
            "SELECT * FROM Budget.BudgetCategories WHERE CategoryId = @CategoryId ORDER BY SortOrder",
            new { CategoryId = categoryId });
        return result.ToList();
    }

    public async Task UpdateSpentAmountAsync(long budgetCategoryId, decimal spentAmount, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var now = DateTime.UtcNow;
        await connection.ExecuteAsync(
            "UPDATE Budget.BudgetCategories SET SpentAmount = @SpentAmount, UpdatedAt = @UpdatedAt WHERE Id = @Id",
            new { Id = budgetCategoryId, SpentAmount = spentAmount, UpdatedAt = now });
    }

    public async Task UpdateBudgetSpentAsync(long? budgetCategoryId = null, long? budgetId = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@BudgetCategoryId", budgetCategoryId, DbType.Int64);
        parameters.Add("@BudgetId", budgetId, DbType.Int64);

        await connection.QueryAsync(
            "Budget.sp_UpdateBudgetSpent",
            parameters,
            commandType: CommandType.StoredProcedure);
    }

    public async Task<long> CreateAsync(BudgetCategory entity, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var now = DateTime.UtcNow;
        var sql = @"
            INSERT INTO Budget.BudgetCategories
                (BudgetId, CategoryId, CustomLabel, AllocatedAmount, SpentAmount, AlertThresholdPct, SortOrder, CreatedAt, UpdatedAt)
            VALUES
                (@BudgetId, @CategoryId, @CustomLabel, @AllocatedAmount, @SpentAmount, @AlertThresholdPct, @SortOrder, @CreatedAt, @UpdatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

        var id = await connection.ExecuteScalarAsync<long>(sql, new
        {
            entity.BudgetId,
            entity.CategoryId,
            entity.CustomLabel,
            entity.AllocatedAmount,
            SpentAmount = entity.SpentAmount,
            entity.AlertThresholdPct,
            entity.SortOrder,
            CreatedAt = now,
            UpdatedAt = now
        });

        entity.Id = id;
        entity.CreatedAt = now;
        entity.UpdatedAt = now;
        return id;
    }


    public async Task<BudgetCategory> AddAsync(BudgetCategory entity, CancellationToken ct = default)
    {
        var id = await CreateAsync(entity, ct);
        entity.Id = id;
        return entity;
    }
    public async Task UpdateAsync(BudgetCategory entity, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var now = DateTime.UtcNow;
        var sql = @"
            UPDATE Budget.BudgetCategories
            SET BudgetId = @BudgetId,
                CategoryId = @CategoryId,
                CustomLabel = @CustomLabel,
                AllocatedAmount = @AllocatedAmount,
                SpentAmount = @SpentAmount,
                AlertThresholdPct = @AlertThresholdPct,
                SortOrder = @SortOrder,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id";

        await connection.ExecuteAsync(sql, new
        {
            entity.Id,
            entity.BudgetId,
            entity.CategoryId,
            entity.CustomLabel,
            entity.AllocatedAmount,
            entity.SpentAmount,
            entity.AlertThresholdPct,
            entity.SortOrder,
            UpdatedAt = now
        });

        entity.UpdatedAt = now;
    }


    public Task RemoveAsync(BudgetCategory entity, CancellationToken ct = default) => Task.CompletedTask;
}
