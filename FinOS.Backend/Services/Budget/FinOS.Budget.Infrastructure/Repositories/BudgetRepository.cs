using System.Data;
using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.Budget.Domain.Entities;
using FinOS.Budget.Domain.Interfaces;

namespace FinOS.Budget.Infrastructure.Repositories;

public class BudgetRepository : IBudgetRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public BudgetRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Domain.Entities.Budget?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var budget = await connection.QueryFirstOrDefaultAsync<Domain.Entities.Budget>(
            "SELECT * FROM Budget.Budgets WHERE Id = @Id AND DeletedAt IS NULL",
            new { Id = id });
        if (budget is not null)
        {
            await LoadCategoriesAsync(connection, budget);
        }
        return budget;
    }

    public async Task<Domain.Entities.Budget?> GetWithCategoriesAsync(long budgetId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var budgetDict = new Dictionary<long, Domain.Entities.Budget>();
        var sql = @"
            SELECT b.*, bc.*, ba.*
            FROM Budget.Budgets b
            LEFT JOIN Budget.BudgetCategories bc ON b.Id = bc.BudgetId
            LEFT JOIN Budget.BudgetAlerts ba ON bc.Id = ba.BudgetCategoryId
            WHERE b.Id = @Id AND b.DeletedAt IS NULL";

        await connection.QueryAsync<Domain.Entities.Budget, BudgetCategory, BudgetAlert, Domain.Entities.Budget>(
            sql,
            (budget, category, alert) =>
            {
                if (!budgetDict.TryGetValue(budget.Id, out var existingBudget))
                {
                    existingBudget = budget;
                    existingBudget.Categories = new List<BudgetCategory>();
                    budgetDict.Add(existingBudget.Id, existingBudget);
                }

                if (category != null)
                {
                    var existingCategory = existingBudget.Categories.FirstOrDefault(c => c.Id == category.Id);
                    if (existingCategory == null)
                    {
                        category.Alerts = new List<BudgetAlert>();
                        existingBudget.Categories.Add(category);
                        existingCategory = category;
                    }
                    if (alert != null) ((List<BudgetAlert>)existingCategory.Alerts).Add(alert);
                }

                return existingBudget;
            },
            new { Id = budgetId },
            splitOn: "Id,Id");

        return budgetDict.Values.FirstOrDefault();
    }

    public async Task<PagedResult<Domain.Entities.Budget>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause)
            ? "WHERE DeletedAt IS NULL"
            : $"WHERE DeletedAt IS NULL AND ({whereClause})";
        var sortDirection = query.SortDirection?.ToLower() == "asc" ? "ASC" : "DESC";
        var sortColumn = !string.IsNullOrWhiteSpace(query.SortBy) ? query.SortBy : "CreatedAt";
        var offset = (query.PageNumber - 1) * query.PageSize;

        var countSql = $"SELECT COUNT(1) FROM [{schema}].[{tableName}] {where}";
        var dataSql = $"""
            SELECT * FROM [{schema}].[{tableName}] {where}
            ORDER BY [{sortColumn}] {sortDirection}
            OFFSET {offset} ROWS FETCH NEXT {query.PageSize} ROWS ONLY
            """;

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param);
        var items = (await connection.QueryAsync<Domain.Entities.Budget>(dataSql, param)).ToList();

        return new PagedResult<Domain.Entities.Budget>
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
            ? "WHERE DeletedAt IS NULL"
            : $"WHERE DeletedAt IS NULL AND ({whereClause})";
        return await connection.ExecuteScalarAsync<long>(
            $"SELECT COUNT(1) FROM [{schema}].[{tableName}] {where}", param);
    }

    public async Task<List<Domain.Entities.Budget>> GetByUserIdAsync(long userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var budgets = (await connection.QueryAsync<Domain.Entities.Budget>(
            "SELECT * FROM Budget.Budgets WHERE UserId = @UserId AND DeletedAt IS NULL ORDER BY CreatedAt DESC",
            new { UserId = userId })).ToList();

        foreach (var budget in budgets)
        {
            await LoadCategoriesAsync(connection, budget);
        }

        return budgets;
    }

    public async Task<List<Domain.Entities.Budget>> GetActiveByUserIdAsync(long userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var budgets = (await connection.QueryAsync<Domain.Entities.Budget>(
            "SELECT * FROM Budget.Budgets WHERE UserId = @UserId AND IsActive = 1 AND DeletedAt IS NULL ORDER BY CreatedAt DESC",
            new { UserId = userId })).ToList();

        foreach (var budget in budgets)
        {
            await LoadCategoriesAsync(connection, budget);
        }

        return budgets;
    }

    public async Task<long> CreateAsync(Domain.Entities.Budget budget, string? categoriesJson = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", budget.UserId, DbType.Int64);
        parameters.Add("@Name", budget.Name, DbType.String, size: 100);
        parameters.Add("@PeriodType", budget.PeriodType.ToString(), DbType.String, size: 20);
        parameters.Add("@StartDate", budget.StartDate, DbType.Date);
        parameters.Add("@EndDate", budget.EndDate, DbType.Date);
        parameters.Add("@TotalBudgetAmount", budget.TotalBudgetAmount, DbType.Decimal);
        parameters.Add("@Currency", budget.Currency, DbType.String, size: 3);
        parameters.Add("@RolloverEnabled", budget.RolloverEnabled, DbType.Boolean);
        parameters.Add("@AlertThresholdPct", budget.AlertThresholdPct, DbType.Decimal);
        parameters.Add("@IsTemplate", budget.IsTemplate, DbType.Boolean);
        parameters.Add("@Categories", categoriesJson, DbType.String, size: -1);
        parameters.Add("@NewBudgetId", dbType: DbType.Int64, direction: ParameterDirection.Output);

        await connection.ExecuteAsync(
            "Budget.sp_CreateBudget",
            parameters,
            commandType: CommandType.StoredProcedure);

        budget.Id = parameters.Get<long>("@NewBudgetId");
        return budget.Id;
    }


    public async Task<Domain.Entities.Budget> AddAsync(Domain.Entities.Budget entity, CancellationToken ct = default)
    {
        var id = await CreateAsync(entity, null, ct);
        entity.Id = id;
        return entity;
    }
    public async Task UpdateAsync(Domain.Entities.Budget budget, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var now = DateTime.UtcNow;
        var sql = @"
            UPDATE Budget.Budgets
            SET Name = @Name,
                PeriodType = @PeriodType,
                StartDate = @StartDate,
                EndDate = @EndDate,
                TotalBudgetAmount = @TotalBudgetAmount,
                Currency = @Currency,
                RolloverEnabled = @RolloverEnabled,
                AlertThresholdPct = @AlertThresholdPct,
                IsTemplate = @IsTemplate,
                IsActive = @IsActive,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id AND DeletedAt IS NULL";

        await connection.ExecuteAsync(sql, new
        {
            budget.Id,
            budget.Name,
            PeriodType = budget.PeriodType.ToString(),
            budget.StartDate,
            budget.EndDate,
            budget.TotalBudgetAmount,
            budget.Currency,
            budget.RolloverEnabled,
            budget.AlertThresholdPct,
            budget.IsTemplate,
            budget.IsActive,
            UpdatedAt = now
        });
    }

    public async Task ReplaceCategoriesAsync(long budgetId, long userId, List<BudgetCategory> categories, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var now = DateTime.UtcNow;
            var ownsBudget = await connection.ExecuteScalarAsync<int>(
                """
                SELECT COUNT(1) FROM Budget.Budgets
                WHERE Id = @budgetId AND UserId = @userId AND DeletedAt IS NULL;
                """, new { budgetId, userId }, transaction: transaction);
            if (ownsBudget != 1)
                throw new InvalidOperationException("Budget was not found.");

            var categoryIds = categories.Where(x => x.CategoryId > 0)
                .Select(x => x.CategoryId).Distinct().ToArray();
            if (categoryIds.Length > 0)
            {
                var validCategoryCount = await connection.ExecuteScalarAsync<int>(
                    """
                    SELECT COUNT(1) FROM Core.Categories
                    WHERE Id IN @categoryIds AND IsActive = 1
                      AND (IsSystem = 1 OR UserId = @userId);
                    """, new { categoryIds, userId }, transaction: transaction);
                if (validCategoryCount != categoryIds.Length)
                    throw new InvalidOperationException("One or more budget categories are unavailable.");
            }

            // Delete existing categories
            await connection.ExecuteAsync(
                "DELETE FROM Budget.BudgetCategories WHERE BudgetId = @BudgetId",
                new { BudgetId = budgetId },
                transaction: transaction);

            // Insert new categories
            foreach (var category in categories)
            {
                var sql = @"
                    INSERT INTO Budget.BudgetCategories
                        (BudgetId, CategoryId, CustomLabel, AllocatedAmount, SpentAmount, AlertThresholdPct, SortOrder, CreatedAt, UpdatedAt)
                    VALUES
                        (@BudgetId, @CategoryId, @CustomLabel, @AllocatedAmount, @SpentAmount, @AlertThresholdPct, @SortOrder, @CreatedAt, @UpdatedAt);
                    SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

                var id = await connection.ExecuteScalarAsync<long>(sql, new
                {
                    BudgetId = budgetId,
                    category.CategoryId,
                    category.CustomLabel,
                    category.AllocatedAmount,
                    SpentAmount = category.SpentAmount,
                    category.AlertThresholdPct,
                    category.SortOrder,
                    CreatedAt = now,
                    UpdatedAt = now
                }, transaction: transaction);

                category.Id = id;
                category.BudgetId = budgetId;
                category.CreatedAt = now;
                category.UpdatedAt = now;
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task SoftDeleteAsync(long budgetId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var now = DateTime.UtcNow;
        await connection.ExecuteAsync(
            "UPDATE Budget.Budgets SET DeletedAt = @DeletedAt, IsActive = 0, UpdatedAt = @UpdatedAt WHERE Id = @Id AND DeletedAt IS NULL",
            new { Id = budgetId, DeletedAt = now, UpdatedAt = now });
    }

    public async Task CheckBudgetAlertsAsync(long userId, long? categoryId = null, decimal? amount = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId, DbType.Int64);
        parameters.Add("@CategoryId", categoryId, DbType.Int64);
        parameters.Add("@Amount", amount, DbType.Decimal);

        await connection.QueryMultipleAsync(
            "Budget.sp_CheckBudgetAlerts",
            parameters,
            commandType: CommandType.StoredProcedure);
    }

    private static async Task LoadCategoriesAsync(Microsoft.Data.SqlClient.SqlConnection connection, Domain.Entities.Budget budget)
    {
        var categoryDict = new Dictionary<long, BudgetCategory>();
        var sql = @"
            SELECT bc.*, ba.*
            FROM Budget.BudgetCategories bc
            LEFT JOIN Budget.BudgetAlerts ba ON bc.Id = ba.BudgetCategoryId
            WHERE bc.BudgetId = @BudgetId
            ORDER BY bc.SortOrder";

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
            new { BudgetId = budget.Id },
            splitOn: "Id");

        budget.Categories = categoryDict.Values.ToList();
    }

    public void Update(Domain.Entities.Budget entity) { }

    public void Remove(Domain.Entities.Budget entity) { }

    public Task RemoveAsync(Domain.Entities.Budget entity, CancellationToken ct = default) => Task.CompletedTask;
}
