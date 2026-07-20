using System.Data;
using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.Loan.Domain.Entities;
using FinOS.Loan.Domain.Interfaces;
using FinOS.Loan.Domain.Results;

namespace FinOS.Loan.Infrastructure.Repositories;

public class EMIScheduleRepository : IEMIScheduleRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public EMIScheduleRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<EMISchedule?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<EMISchedule>(
            "SELECT * FROM Loan.EMISchedule WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<PagedResult<EMISchedule>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $"WHERE {whereClause}";
        var sortDirection = query.SortDirection?.ToLower() == "asc" ? "ASC" : "DESC";
        var sortColumn = !string.IsNullOrWhiteSpace(query.SortBy) ? query.SortBy : "EMINumber";
        var offset = (query.PageNumber - 1) * query.PageSize;

        var countSql = $"SELECT COUNT(1) FROM [{schema}].[{tableName}] {where}";
        var dataSql = $"""
            SELECT * FROM [{schema}].[{tableName}] {where}
            ORDER BY [{sortColumn}] {sortDirection}
            OFFSET {offset} ROWS FETCH NEXT {query.PageSize} ROWS ONLY
            """;

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param);
        var items = (await connection.QueryAsync<EMISchedule>(dataSql, param)).ToList();

        return new PagedResult<EMISchedule> { Items = items, TotalCount = totalCount, Page = query.PageNumber, PageSize = query.PageSize };
    }

    public async Task<long> CountAsync(string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $"WHERE {whereClause}";
        return await connection.ExecuteScalarAsync<long>($"SELECT COUNT(1) FROM [{schema}].[{tableName}] {where}", param);
    }

    public async Task<List<EMISchedule>> GetByLoanIdAsync(long loanId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<EMISchedule>(
            "SELECT * FROM Loan.EMISchedule WHERE LoanId = @LoanId ORDER BY EMINumber",
            new { LoanId = loanId });
        return result.ToList();
    }

    public async Task<List<EMISchedule>> GetUpcomingEMIsAsync(long loanId, int count = 3, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<EMISchedule>(
            "SELECT TOP (@Count) * FROM Loan.EMISchedule WHERE LoanId = @LoanId AND IsPaid = 0 ORDER BY EMIDate",
            new { LoanId = loanId, Count = count });
        return result.ToList();
    }

    public async Task<EMISchedule?> GetNextUnpaidEMIAsync(long loanId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<EMISchedule>(
            "SELECT TOP 1 * FROM Loan.EMISchedule WHERE LoanId = @LoanId AND IsPaid = 0 ORDER BY EMINumber",
            new { LoanId = loanId });
    }

    /// <summary>
    /// Records an EMI payment using the Loan.sp_RecordEMIPayment stored procedure.
    /// The SP handles marking the EMI as paid, updating loan outstanding, and debiting the linked account.
    /// </summary>
    public async Task<EMIPaymentResult> RecordEMIPaymentAsync(long loanId, int emiNumber, DateTime? paidDate = null, decimal? paidAmount = null, decimal lateFee = 0, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@LoanId", loanId, DbType.Int64);
        parameters.Add("@EMINumber", emiNumber, DbType.Int32);
        parameters.Add("@PaidDate", paidDate, DbType.Date);
        parameters.Add("@PaidAmount", paidAmount, DbType.Decimal, precision: 18, scale: 2);
        parameters.Add("@LateFee", lateFee, DbType.Decimal, precision: 18, scale: 2);

        return await connection.QueryFirstOrDefaultAsync<EMIPaymentResult>(
            "Loan.sp_RecordEMIPayment", parameters, commandType: CommandType.StoredProcedure)
            ?? new EMIPaymentResult();
    }

    public Task<EMISchedule> AddAsync(EMISchedule entity, CancellationToken ct = default)
    {
        throw new NotImplementedException("EMI schedules are generated via LoanRepository.GenerateAmortizationScheduleAsync.");
    }

    public Task UpdateAsync(EMISchedule entity, CancellationToken ct = default) => Task.CompletedTask;

    public Task RemoveAsync(EMISchedule entity, CancellationToken ct = default) => Task.CompletedTask;
}


