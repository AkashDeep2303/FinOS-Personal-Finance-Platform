using FinOS.Common.Interfaces;

namespace FinOS.Loan.Domain.Interfaces;

public interface ILoanRepository : IRepository<Domain.Entities.Loan>
{
    Task<List<Domain.Entities.Loan>> GetByUserIdAsync(long userId, CancellationToken ct = default);
    Task<Domain.Entities.Loan?> GetWithScheduleAsync(long loanId, CancellationToken ct = default);
    Task<Domain.Entities.Loan?> GetWithPrepaymentsAsync(long loanId, CancellationToken ct = default);
    Task<List<Domain.Entities.Loan>> GetActiveByUserIdAsync(long userId, CancellationToken ct = default);
    Task<Domain.Results.DebtOverviewResult> GetDebtOverviewAsync(long userId, CancellationToken ct = default);
    Task<List<Domain.Results.LoanRateHistoryResult>> GetRateHistoryAsync(long loanId, CancellationToken ct = default);
    Task AddRateChangeAsync(long loanId, decimal newRate, DateTime effectiveDate, string? reason, CancellationToken ct = default);
    Task<Domain.Results.LoanPaymentAnalysisResult> GetPaymentAnalysisAsync(long loanId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new loan using the Loan.sp_CreateLoan stored procedure.
    /// The SP calculates EMI, total interest, maturity date, and first EMI date.
    /// Returns the newly created loan ID.
    /// </summary>
    Task<long> CreateAsync(Domain.Entities.Loan loan, CancellationToken ct = default);

    /// <summary>
    /// Generates the full amortization schedule for a loan using Loan.sp_GenerateAmortizationSchedule.
    /// The SP deletes existing unpaid EMIs and regenerates the schedule.
    /// </summary>
    Task GenerateAmortizationScheduleAsync(long loanId, CancellationToken ct = default);

    /// <summary>
    /// Closes a loan by setting Status = Closed, clearing outstanding and tenure.
    /// </summary>
    Task CloseLoanAsync(long loanId, CancellationToken ct = default);
}
