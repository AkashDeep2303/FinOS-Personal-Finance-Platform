using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.CoreFinance.Domain.Entities;
using FinOS.CoreFinance.Domain.Interfaces;

namespace FinOS.CoreFinance.Infrastructure.Repositories;

public class RecurringScheduleRepository : IRecurringScheduleRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public RecurringScheduleRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<RecurringSchedule?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT rs.*, a.*, c.*
            FROM Core.RecurringSchedules rs
            LEFT JOIN Core.Accounts a ON rs.AccountId = a.Id
            LEFT JOIN Core.Categories c ON rs.CategoryId = c.Id
            WHERE rs.Id = @Id";
        var result = await connection.QueryAsync<RecurringSchedule, Account, Category, RecurringSchedule>(sql,
            (schedule, account, category) => { schedule.Account = account; schedule.Category = category; return schedule; },
            new { Id = id }, splitOn: "Id,Id");
        return result.FirstOrDefault();
    }

    public async Task<PagedResult<RecurringSchedule>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "WHERE DeletedAt IS NULL" : $"WHERE DeletedAt IS NULL AND ({whereClause})";
        var offset = (query.PageNumber - 1) * query.PageSize;
        var countSql = $"SELECT COUNT(1) FROM [{schema}].[{tableName}] {where}";
        var dataSql = $"""
            SELECT * FROM [{schema}].[{tableName}] {where}
            ORDER BY NextOccurrenceDate ASC
            OFFSET {offset} ROWS FETCH NEXT {query.PageSize} ROWS ONLY
            """;
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param);
        var items = (await connection.QueryAsync<RecurringSchedule>(dataSql, param)).ToList();
        return new PagedResult<RecurringSchedule> { Items = items, TotalCount = totalCount, Page = query.PageNumber, PageSize = query.PageSize };
    }

    public async Task<long> CountAsync(string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "WHERE DeletedAt IS NULL" : $"WHERE DeletedAt IS NULL AND ({whereClause})";
        return await connection.ExecuteScalarAsync<long>($"SELECT COUNT(1) FROM [{schema}].[{tableName}] {where}", param);
    }

    public async Task<List<RecurringSchedule>> GetByUserIdAsync(long userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<RecurringSchedule>(
            "SELECT * FROM Core.RecurringSchedules WHERE UserId = @UserId ORDER BY NextOccurrenceDate",
            new { UserId = userId });
        return result.ToList();
    }

    public async Task<List<RecurringSchedule>> GetDueSchedulesAsync(CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var now = DateTime.UtcNow;
        var result = await connection.QueryAsync<RecurringSchedule>(
            "SELECT * FROM Core.RecurringSchedules WHERE IsActive = 1 AND AutoCreate = 1 AND NextOccurrenceDate <= @Now AND (EndDate IS NULL OR EndDate >= @Now) ORDER BY NextOccurrenceDate",
            new { Now = now });
        return result.ToList();
    }

    public async Task<long> CreateAsync(RecurringSchedule entity, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", entity.UserId);
        parameters.Add("@AccountId", entity.AccountId);
        parameters.Add("@CategoryId", entity.CategoryId);
        parameters.Add("@Name", entity.Name);
        parameters.Add("@Frequency", entity.Frequency);
        parameters.Add("@Amount", entity.Amount);
        parameters.Add("@Currency", entity.Currency);
        parameters.Add("@NextOccurrenceDate", entity.NextOccurrenceDate);
        parameters.Add("@EndDate", entity.EndDate);
        parameters.Add("@AutoCreate", entity.AutoCreate);
        parameters.Add("@Description", entity.Description);
        parameters.Add("@IsActive", entity.IsActive);
        parameters.Add("@NewRecurringScheduleId", dbType: System.Data.DbType.Int64, direction: System.Data.ParameterDirection.Output);

        await connection.ExecuteAsync("Core.sp_CreateRecurringSchedule", parameters, commandType: System.Data.CommandType.StoredProcedure);
        return parameters.Get<long>("@NewRecurringScheduleId");
    }

    public async Task UpdateAsync(RecurringSchedule entity, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"UPDATE Core.RecurringSchedules SET Name=@Name, Frequency=@Frequency, Amount=@Amount,
            Currency=@Currency, NextOccurrenceDate=@NextOccurrenceDate, EndDate=@EndDate, AutoCreate=@AutoCreate,
            Description=@Description, IsActive=@IsActive, UpdatedAt=@UpdatedAt WHERE Id=@Id";
        await connection.ExecuteAsync(sql, entity);
    }

    public async Task<RecurringSchedule> AddAsync(RecurringSchedule entity, CancellationToken ct = default)
    {
        var id = await CreateAsync(entity, ct);
        entity.Id = id;
        return entity;
    }

    public Task RemoveAsync(RecurringSchedule entity, CancellationToken ct = default) => Task.CompletedTask;
}
