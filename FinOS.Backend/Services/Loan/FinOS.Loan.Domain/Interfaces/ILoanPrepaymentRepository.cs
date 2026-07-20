using FinOS.Common.Interfaces;
using FinOS.Loan.Domain.Results;

namespace FinOS.Loan.Domain.Interfaces;

public interface ILoanPrepaymentRepository : IRepository<Domain.Entities.LoanPrepayment>
{
    Task<List<Domain.Entities.LoanPrepayment>> GetByLoanIdAsync(long loanId, CancellationToken ct = default);

    /// <summary>
    /// Simulates a prepayment using the Loan.sp_SimulatePrepayment stored procedure.
    /// The SP calculates what-if scenarios (reduce EMI vs reduce tenure) without persisting changes.
    /// </summary>
    Task<PrepaymentSimulationResult> SimulatePrepaymentAsync(
        long loanId, decimal prepaymentAmount, string strategy, DateTime? prepaymentDate = null, CancellationToken ct = default);

    /// <summary>
    /// Executes a prepayment using the Loan.sp_ExecutePrepayment stored procedure.
    /// The SP handles recording the prepayment, updating loan details, debiting the account,
    /// and regenerating the amortization schedule.
    /// </summary>
    Task<PrepaymentExecutionResult> ExecutePrepaymentAsync(
        long loanId, decimal prepaymentAmount, string strategy, DateTime? prepaymentDate = null, string? notes = null, CancellationToken ct = default);
}
