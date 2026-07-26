using System.Data;
using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.Loan.Domain.Entities;
using FinOS.Loan.Domain.Enums;
using FinOS.Loan.Domain.Interfaces;

namespace FinOS.Loan.Infrastructure.Repositories;

public class LoanRepository : ILoanRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public LoanRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Domain.Entities.Loan?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var loan = await connection.QueryFirstOrDefaultAsync<Domain.Entities.Loan>(
            "SELECT * FROM Loan.Loans WHERE Id = @Id AND DeletedAt IS NULL",
            new { Id = id });
        if (loan is not null) await LoadLoanTypeAsync(connection, loan);
        return loan;
    }

    public async Task<PagedResult<Domain.Entities.Loan>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "WHERE DeletedAt IS NULL" : $"WHERE DeletedAt IS NULL AND ({whereClause})";
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
        var items = (await connection.QueryAsync<Domain.Entities.Loan>(dataSql, param)).ToList();

        return new PagedResult<Domain.Entities.Loan> { Items = items, TotalCount = totalCount, Page = query.PageNumber, PageSize = query.PageSize };
    }

    public async Task<long> CountAsync(string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "WHERE DeletedAt IS NULL" : $"WHERE DeletedAt IS NULL AND ({whereClause})";
        return await connection.ExecuteScalarAsync<long>($"SELECT COUNT(1) FROM [{schema}].[{tableName}] {where}", param);
    }

    public async Task<List<Domain.Entities.Loan>> GetByUserIdAsync(long userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<Domain.Entities.Loan>(
            "SELECT * FROM Loan.Loans WHERE UserId = @UserId AND DeletedAt IS NULL ORDER BY CreatedAt DESC",
            new { UserId = userId });
        var loans = result.ToList();
        foreach (var loan in loans) await LoadLoanTypeAsync(connection, loan);
        return loans;
    }

    public async Task<Domain.Entities.Loan?> GetWithScheduleAsync(long loanId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT * FROM Loan.Loans WHERE Id = @LoanId AND DeletedAt IS NULL;
            SELECT * FROM Loan.EMISchedule WHERE LoanId = @LoanId ORDER BY EMINumber;";
        using var multi = await connection.QueryMultipleAsync(sql, new { LoanId = loanId });
        var loan = await multi.ReadFirstOrDefaultAsync<Domain.Entities.Loan>();
        if (loan is not null)
        {
            await LoadLoanTypeAsync(connection, loan);
            var schedule = (await multi.ReadAsync<EMISchedule>()).ToList();
            loan.EMISchedule = schedule;
        }
        return loan;
    }

    public async Task<Domain.Entities.Loan?> GetWithPrepaymentsAsync(long loanId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT * FROM Loan.Loans WHERE Id = @LoanId AND DeletedAt IS NULL;
            SELECT * FROM Loan.EMISchedule WHERE LoanId = @LoanId ORDER BY EMINumber;
            SELECT * FROM Loan.LoanPrepayments WHERE LoanId = @LoanId ORDER BY PrepaymentDate DESC;";
        using var multi = await connection.QueryMultipleAsync(sql, new { LoanId = loanId });
        var loan = await multi.ReadFirstOrDefaultAsync<Domain.Entities.Loan>();
        if (loan is not null)
        {
            await LoadLoanTypeAsync(connection, loan);
            loan.EMISchedule = (await multi.ReadAsync<EMISchedule>()).ToList();
            loan.Prepayments = (await multi.ReadAsync<LoanPrepayment>()).ToList();
        }
        return loan;
    }

    public async Task<List<Domain.Entities.Loan>> GetActiveByUserIdAsync(long userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<Domain.Entities.Loan>(
            "SELECT * FROM Loan.Loans WHERE UserId = @UserId AND Status = @Status AND DeletedAt IS NULL ORDER BY CreatedAt DESC",
            new { UserId = userId, Status = LoanStatus.Active.ToString() });
        var loans = result.ToList();
        foreach (var loan in loans) await LoadLoanTypeAsync(connection, loan);
        return loans;
    }

    public async Task<Domain.Results.DebtOverviewResult> GetDebtOverviewAsync(long userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Domain.Results.DebtOverviewResult>(
            """
            SELECT d.TotalOutstandingDebt, d.TotalMonthlyEMI, d.ActiveLoanCount,
                   d.MonthlyIncome, d.DebtToIncomeRatioPct, d.RiskCategory,
                   d.MonthlySurplusAfterEMI,
                   CAST(ISNULL((
                       SELECT SUM(l.OutstandingPrincipal * l.InterestRate)
                            / NULLIF(SUM(l.OutstandingPrincipal), 0)
                       FROM Loan.Loans l
                       WHERE l.UserId = @UserId AND l.Status = N'Active' AND l.DeletedAt IS NULL
                   ), 0) AS DECIMAL(8,4)) AS WeightedInterestRate,
                   (SELECT MAX(l.MaturityDate) FROM Loan.Loans l
                    WHERE l.UserId = @UserId AND l.Status = N'Active' AND l.DeletedAt IS NULL) AS DebtFreeDate
            FROM Views.vw_DebtToIncomeRatio d WHERE d.UserId = @UserId
            """, new { UserId = userId })
            ?? new Domain.Results.DebtOverviewResult();
    }

    public async Task<List<Domain.Results.LoanRateHistoryResult>> GetRateHistoryAsync(long loanId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<Domain.Results.LoanRateHistoryResult>(new CommandDefinition(
            @"SELECT Id, LoanId, PreviousRate, NewRate, EffectiveDate, Reason, CreatedAt
              FROM Loan.LoanInterestRateHistory WHERE LoanId = @LoanId ORDER BY EffectiveDate DESC, Id DESC",
            new { LoanId = loanId }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task AddRateChangeAsync(long loanId, decimal newRate, DateTime effectiveDate, string? reason, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        using var transaction = connection.BeginTransaction();
        var previousRate = await connection.ExecuteScalarAsync<decimal>(new CommandDefinition(
            "SELECT InterestRate FROM Loan.Loans WITH (UPDLOCK, ROWLOCK) WHERE Id = @LoanId AND DeletedAt IS NULL",
            new { LoanId = loanId }, transaction, cancellationToken: ct));
        await connection.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO Loan.LoanInterestRateHistory (LoanId, PreviousRate, NewRate, EffectiveDate, Reason)
              VALUES (@LoanId, @PreviousRate, @NewRate, @EffectiveDate, @Reason);
              UPDATE Loan.Loans SET InterestRate = @NewRate, UpdatedAt = SYSUTCDATETIME() WHERE Id = @LoanId;",
            new { LoanId = loanId, PreviousRate = previousRate, NewRate = newRate, EffectiveDate = effectiveDate.Date, Reason = reason },
            transaction, cancellationToken: ct));
        transaction.Commit();
    }

    public async Task<Domain.Results.LoanPaymentAnalysisResult> GetPaymentAnalysisAsync(long loanId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<Domain.Results.LoanPaymentAnalysisResult>(new CommandDefinition(
            """
            SELECT COUNT(1) ScheduledPayments,
                   ISNULL(SUM(CASE WHEN IsPaid = 1 THEN 1 ELSE 0 END),0) PaidPayments,
                   ISNULL(SUM(CASE WHEN IsPaid = 0 AND EMIDate >= CAST(GETUTCDATE() AS DATE) THEN 1 ELSE 0 END),0) UpcomingPayments,
                   ISNULL(SUM(CASE WHEN IsPaid = 0 AND EMIDate < CAST(GETUTCDATE() AS DATE) THEN 1 ELSE 0 END),0) LatePayments,
                   ISNULL(SUM(PrincipalComponent),0) ScheduledPrincipal,
                   ISNULL(SUM(CASE WHEN IsPaid=1 THEN COALESCE(ActualPrincipalPaid, PrincipalComponent) ELSE 0 END),0) PrincipalPaid,
                   ISNULL(SUM(InterestComponent),0) ScheduledInterest,
                   ISNULL(SUM(CASE WHEN IsPaid=1 THEN COALESCE(ActualInterestPaid, InterestComponent) ELSE 0 END),0) InterestPaid,
                   ISNULL(SUM(CASE WHEN IsPaid=1 THEN LateFee ELSE 0 END),0) LateFeesPaid,
                   ISNULL(SUM(CASE WHEN IsPaid=0 THEN InterestComponent ELSE 0 END),0) RemainingInterest
            FROM Loan.EMISchedule WHERE LoanId = @LoanId
            """, new { LoanId = loanId }, cancellationToken: ct));
    }

    /// <summary>
    /// Creates a new loan using the Loan.sp_CreateLoan stored procedure.
    /// The SP calculates EMI, total interest, maturity date, and first EMI date.
    /// Returns the newly created loan ID.
    /// </summary>
    public async Task<long> CreateAsync(Domain.Entities.Loan loan, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", loan.UserId, DbType.Int64);
        parameters.Add("@LoanTypeId", loan.LoanTypeId, DbType.Int32);
        parameters.Add("@AccountId", loan.AccountId ?? 0, DbType.Int64);
        parameters.Add("@LenderName", loan.LenderName, DbType.String, size: 200);
        parameters.Add("@LoanAccountNumber", loan.LoanAccountNumber, DbType.String, size: 50);
        parameters.Add("@PrincipalAmount", loan.PrincipalAmount, DbType.Decimal, precision: 18, scale: 2);
        parameters.Add("@InterestRate", loan.InterestRate, DbType.Decimal, precision: 8, scale: 4);
        parameters.Add("@InterestType", loan.InterestType.ToString(), DbType.String, size: 20);
        parameters.Add("@TenureMonths", loan.TenureMonths, DbType.Int32);
        parameters.Add("@EMIDayOfMonth", loan.EMIDayOfMonth, DbType.Int32);
        parameters.Add("@StartDate", loan.StartDate, DbType.Date);
        parameters.Add("@ProcessingFee", loan.ProcessingFee, DbType.Decimal, precision: 18, scale: 2);
        parameters.Add("@PrepaymentPenaltyPct", loan.PrepaymentPenaltyPct, DbType.Decimal, precision: 5, scale: 2);
        parameters.Add("@IsPrepaymentAllowed", loan.IsPrepaymentAllowed, DbType.Boolean);
        parameters.Add("@Notes", loan.Notes, DbType.String, size: 500);
        parameters.Add("@NewLoanId", dbType: DbType.Int64, direction: ParameterDirection.Output);

        await connection.ExecuteAsync("Loan.sp_CreateLoan", parameters, commandType: CommandType.StoredProcedure);
        return parameters.Get<long>("@NewLoanId");
    }

    /// <summary>
    /// Generates the full amortization schedule for a loan using Loan.sp_GenerateAmortizationSchedule.
    /// The SP deletes existing unpaid EMIs and regenerates the schedule.
    /// </summary>
    public async Task GenerateAmortizationScheduleAsync(long loanId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@LoanId", loanId, DbType.Int64);

        await connection.ExecuteAsync("Loan.sp_GenerateAmortizationSchedule", parameters, commandType: CommandType.StoredProcedure);
    }

    /// <summary>
    /// Updates a loan using a direct SQL UPDATE. For complex operations
    /// (prepayment, EMI payment), use the dedicated stored procedures instead.
    /// </summary>
    public async Task UpdateAsync(Domain.Entities.Loan loan, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@Id", loan.Id, DbType.Int64);
        parameters.Add("@OutstandingPrincipal", loan.OutstandingPrincipal, DbType.Decimal, precision: 18, scale: 2);
        parameters.Add("@InterestRate", loan.InterestRate, DbType.Decimal, precision: 8, scale: 4);
        parameters.Add("@EMI", loan.EMI, DbType.Decimal, precision: 18, scale: 2);
        parameters.Add("@RemainingTenureMonths", loan.RemainingTenureMonths, DbType.Int32);
        parameters.Add("@TotalPaid", loan.TotalPaid, DbType.Decimal, precision: 18, scale: 2);
        parameters.Add("@TotalInterestPaid", loan.TotalInterestPaid, DbType.Decimal, precision: 18, scale: 2);
        parameters.Add("@TotalPrepaid", loan.TotalPrepaid, DbType.Decimal, precision: 18, scale: 2);
        parameters.Add("@NextEMIDate", loan.NextEMIDate, DbType.DateTime2);
        parameters.Add("@Status", loan.Status.ToString(), DbType.String, size: 20);
        parameters.Add("@MaturityDate", loan.MaturityDate, DbType.DateTime2);
        parameters.Add("@Notes", loan.Notes, DbType.String, size: 500);
        parameters.Add("@UpdatedAt", DateTime.UtcNow, DbType.DateTime2);

        await connection.ExecuteAsync(@"
            UPDATE Loan.Loans SET
                OutstandingPrincipal = @OutstandingPrincipal,
                InterestRate = @InterestRate,
                EMI = @EMI,
                RemainingTenureMonths = @RemainingTenureMonths,
                TotalPaid = @TotalPaid,
                TotalInterestPaid = @TotalInterestPaid,
                TotalPrepaid = @TotalPrepaid,
                NextEMIDate = @NextEMIDate,
                Status = @Status,
                MaturityDate = @MaturityDate,
                Notes = @Notes,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id", parameters);
    }

    /// <summary>
    /// Closes a loan by setting Status = Closed, clearing outstanding and tenure.
    /// </summary>
    public async Task CloseLoanAsync(long loanId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            UPDATE Loan.Loans SET
                Status = N'Closed',
                OutstandingPrincipal = 0,
                RemainingTenureMonths = 0,
                NextEMIDate = NULL,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id AND DeletedAt IS NULL",
            new { Id = loanId, UpdatedAt = DateTime.UtcNow });
    }

    /// <summary>
    /// Soft-deletes a loan by setting DeletedAt timestamp.
    /// </summary>
    public async Task SoftDeleteAsync(long loanId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            UPDATE Loan.Loans SET DeletedAt = @DeletedAt, UpdatedAt = @UpdatedAt
            WHERE Id = @Id AND DeletedAt IS NULL",
            new { Id = loanId, DeletedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
    }

    // Ã¢â€â‚¬Ã¢â€â‚¬ Private helpers Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬

    private static async Task LoadLoanTypeAsync(Microsoft.Data.SqlClient.SqlConnection connection, Domain.Entities.Loan loan)
    {
        var loanType = await connection.QueryFirstOrDefaultAsync<LoanType>(
            "SELECT * FROM Loan.LoanTypes WHERE Id = @Id",
            new { Id = loan.LoanTypeId });
        loan.LoanType = loanType!;
    }

    public async Task<Domain.Entities.Loan> AddAsync(Domain.Entities.Loan entity, CancellationToken ct = default)
    {
        var id = await CreateAsync(entity, ct);
        entity.Id = id;
        return entity;
    }

    public void Update(Domain.Entities.Loan entity) { }

    public Task RemoveAsync(Domain.Entities.Loan entity, CancellationToken ct = default) => Task.CompletedTask;
}
