using FinOS.Analytics.Domain.Results;

namespace FinOS.Analytics.Domain.Interfaces;

public interface ICashFlowClassificationRepository
{
    Task<CashFlowClassificationResult> GetForMonthAsync(
        long userId, DateTime monthStart, DateTime monthEnd, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MonthlyCashFlowResult>> GetHistoryAsync(
        long userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
