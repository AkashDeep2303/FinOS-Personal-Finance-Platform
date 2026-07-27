using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.CoreFinance.Domain.Entities;
using FinOS.CoreFinance.Domain.Enums;
using FinOS.CoreFinance.Domain.Interfaces;

namespace FinOS.CoreFinance.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public CategoryRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Category?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT c.*, p.* FROM Core.Categories c
            LEFT JOIN Core.Categories p ON c.ParentId = p.Id
            WHERE c.Id = @Id";
        var catDict = new Dictionary<long, Category>();
        await connection.QueryAsync<Category, Category, Category>(sql,
            (cat, parent) => { cat.Parent = parent; catDict[cat.Id] = cat; return cat; },
            new { Id = id }, splitOn: "Id");
        var category = catDict.Values.FirstOrDefault();
        if (category != null)
        {
            var children = await connection.QueryAsync<Category>(
                "SELECT * FROM Core.Categories WHERE ParentId = @Id ORDER BY SortOrder, Name", new { Id = id });
            category.Children = children.ToList();
        }
        return category;
    }

    public async Task<PagedResult<Category>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "WHERE DeletedAt IS NULL" : $"WHERE DeletedAt IS NULL AND ({whereClause})";
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
        var items = (await connection.QueryAsync<Category>(dataSql, param)).ToList();
        return new PagedResult<Category> { Items = items, TotalCount = totalCount, Page = query.PageNumber, PageSize = query.PageSize };
    }

    public async Task<long> CountAsync(string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "WHERE DeletedAt IS NULL" : $"WHERE DeletedAt IS NULL AND ({whereClause})";
        return await connection.ExecuteScalarAsync<long>($"SELECT COUNT(1) FROM [{schema}].[{tableName}] {where}", param);
    }

    public async Task<List<Category>> GetByUserIdAsync(long userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<Category>(
            "SELECT * FROM Core.Categories WHERE (UserId = @UserId OR IsSystem = 1) ORDER BY SortOrder, Name",
            new { UserId = userId });
        return result.ToList();
    }

    public async Task<List<Category>> GetByUserIdAndTypeAsync(long userId, CategoryType type, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<Category>(
            "SELECT * FROM Core.Categories WHERE (UserId = @UserId OR IsSystem = 1) AND Type = @Type ORDER BY SortOrder, Name",
            new { UserId = userId, Type = type.ToString() });
        return result.ToList();
    }

    public async Task<List<Category>> GetSystemCategoriesAsync(CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<Category>(
            "SELECT * FROM Core.Categories WHERE IsSystem = 1 ORDER BY Type, SortOrder");
        return result.ToList();
    }

    public async Task<Category> AddAsync(Category entity, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO Core.Categories (UserId, ParentId, Name, Icon, Color, Type, SortOrder, IsSystem, BudgetAmount, CashFlowClassification, CreatedAt, UpdatedAt)
            VALUES (@UserId, @ParentId, @Name, @Icon, @Color, @Type, @SortOrder, @IsSystem, @BudgetAmount, @CashFlowClassification, @CreatedAt, @UpdatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
        var id = await connection.ExecuteScalarAsync<long>(sql, entity);
        entity.Id = id;
        return entity;
    }

    public async Task UpdateAsync(Category entity, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE Core.Categories
            SET Name = @Name, Icon = @Icon, Color = @Color, BudgetAmount = @BudgetAmount,
                IsActive = @IsActive, SortOrder = @SortOrder,
                CashFlowClassification = @CashFlowClassification,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id AND UserId = @UserId AND IsSystem = 0;
            """;
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, entity, cancellationToken: ct));
    }
    public Task RemoveAsync(Category entity, CancellationToken ct = default) => Task.CompletedTask;
}
