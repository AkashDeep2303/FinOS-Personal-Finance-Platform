using FinOS.Common.Interfaces;
using FinOS.Loan.Domain.Results;

namespace FinOS.Loan.Domain.Interfaces;

public interface IEMIScheduleRepository : IRepository<Domain.Entities.EMISchedule>
{
    Task<List<Domain.Entities.EMISchedule>> GetByLoanIdAsync(long loanId, CancellationToken ct = default);
    Task<List<Domain.Entities.EMISchedule>> GetUpcomingEMIsAsync(long loanId, int count = 3, CancellationToken ct = default);
    Task<Domain.Entities.EMISchedule?> GetNextUnpaidEMIAsync(long loanId, CancellationToken ct = default);

    /// <summary>
    /// Records an EMI payment using the Loan.sp_RecordEMIPayment stored procedure.
    /// The SP handles marking the EMI as paid, updating loan outstanding, and debiting the linked account.
    /// </summary>
    Task<EMIPaymentResult> RecordEMIPaymentAsync(long loanId, int emiNumber, DateTime? paidDate = null, decimal? paidAmount = null, decimal lateFee = 0, CancellationToken ct = default);
}
