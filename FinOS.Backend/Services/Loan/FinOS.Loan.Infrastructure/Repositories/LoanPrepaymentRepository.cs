using System.Data;
using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.Loan.Domain.Entities;
using FinOS.Loan.Domain.Interfaces;
using FinOS.Loan.Domain.Results;

namespace FinOS.Loan.Infrastructure.Repositories;

public class LoanPrepaymentRepository : ILoanPrepaymentRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public LoanPrepaymentRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<LoanPrepayment?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<LoanPrepayment>(
            "SELECT * FROM Loan.LoanPrepayments WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<PagedResult<LoanPrepayment>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $"WHERE {whereClause}";
        var sortDirection = query.SortDirection?.ToLower() == "asc" ? "ASC" : "DESC";
        var sortColumn = !string.IsNullOrWhiteSpace(query.SortBy) ? query.SortBy : "PrepaymentDate";
        var offset = (query.PageNumber - 1) * query.PageSize;

        var countSql = $"SELECT COUNT(1) FROM [{schema}].[{tableName}] {where}";
        var dataSql = $"""
            SELECT * FROM [{schema}].[{tableName}] {where}
            ORDER BY [{sortColumn}] {sortDirection}
            OFFSET {offset} ROWS FETCH NEXT {query.PageSize} ROWS ONLY
            """;

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param);
        var items = (await connection.QueryAsync<LoanPrepayment>(dataSql, param)).ToList();

        return new PagedResult<LoanPrepayment> { Items = items, TotalCount = totalCount, Page = query.PageNumber, PageSize = query.PageSize };
    }

    public async Task<long> CountAsync(string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $"WHERE {whereClause}";
        return await connection.ExecuteScalarAsync<long>($"SELECT COUNT(1) FROM [{schema}].[{tableName}] {where}", param);
    }

    public async Task<List<LoanPrepayment>> GetByLoanIdAsync(long loanId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<LoanPrepayment>(
            "SELECT * FROM Loan.LoanPrepayments WHERE LoanId = @LoanId ORDER BY PrepaymentDate DESC",
            new { LoanId = loanId });
        return result.ToList();
    }

    /// <summary>
    /// Simulates a prepayment using the Loan.sp_SimulatePrepayment stored procedure.
    /// The SP calculates what-if scenarios (reduce EMI vs reduce tenure) without persisting changes.
    /// Results are saved to Loan.PrepaymentSimulations table by the SP.
    /// </summary>
    public async Task<PrepaymentSimulationResult> SimulatePrepaymentAsync(
        long loanId, decimal prepaymentAmount, string strategy, DateTime? prepaymentDate = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@LoanId", loanId, DbType.Int64);
        parameters.Add("@PrepaymentAmount", prepaymentAmount, DbType.Decimal, precision: 18, scale: 2);
        parameters.Add("@PrepaymentDate", prepaymentDate, DbType.Date);
        parameters.Add("@Strategy", strategy, DbType.String, size: 30);

        return await connection.QueryFirstOrDefaultAsync<PrepaymentSimulationResult>(
            "Loan.sp_SimulatePrepayment", parameters, commandType: CommandType.StoredProcedure)
            ?? new PrepaymentSimulationResult();
    }

    /// <summary>
    /// Executes a prepayment using the Loan.sp_ExecutePrepayment stored procedure.
    /// The SP handles recording the prepayment, updating loan details, debiting the account,
    /// and regenerating the amortization schedule.
    /// </summary>
    public async Task<PrepaymentExecutionResult> ExecutePrepaymentAsync(
        long loanId, decimal prepaymentAmount, string strategy, DateTime? prepaymentDate = null, string? notes = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@LoanId", loanId, DbType.Int64);
        parameters.Add("@PrepaymentAmount", prepaymentAmount, DbType.Decimal, precision: 18, scale: 2);
        parameters.Add("@Strategy", strategy, DbType.String, size: 30);
        parameters.Add("@PrepaymentDate", prepaymentDate, DbType.Date);
        parameters.Add("@Notes", notes, DbType.String, size: 500);

        return await connection.QueryFirstOrDefaultAsync<PrepaymentExecutionResult>(
            "Loan.sp_ExecutePrepayment", parameters, commandType: CommandType.StoredProcedure)
            ?? new PrepaymentExecutionResult();
    }

    public Task<LoanPrepayment> AddAsync(LoanPrepayment entity, CancellationToken ct = default)
    {
        throw new NotImplementedException("Loan prepayments are created via ExecutePrepaymentAsync.");
    }

    public Task UpdateAsync(LoanPrepayment entity, CancellationToken ct = default) => Task.CompletedTask;

    public Task RemoveAsync(LoanPrepayment entity, CancellationToken ct = default) => Task.CompletedTask;
}


