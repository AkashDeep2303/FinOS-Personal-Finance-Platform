using FinOS.Common.Interfaces;
using FinOS.CoreFinance.Application.DTOs;
using FinOS.CoreFinance.Application.Services;
using FinOS.CoreFinance.Domain.Entities;
using FinOS.CoreFinance.Domain.Enums;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FinOS.CoreFinance.Application.Commands;

public class ProcessRecurringTransactionsCommand : IRequest<int>
{
    // Returns count of processed transactions
}

public class ProcessRecurringTransactionsCommandHandler : IRequestHandler<ProcessRecurringTransactionsCommand, int>
{
    private readonly IRecurringScheduleRepository _scheduleRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IBalanceUpdateService _balanceUpdateService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessRecurringTransactionsCommandHandler> _logger;

    public ProcessRecurringTransactionsCommandHandler(
        IRecurringScheduleRepository scheduleRepository,
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        IBalanceUpdateService balanceUpdateService,
        IUnitOfWork unitOfWork,
        ILogger<ProcessRecurringTransactionsCommandHandler> logger)
    {
        _scheduleRepository = scheduleRepository;
        _transactionRepository = transactionRepository;
        _accountRepository = accountRepository;
        _balanceUpdateService = balanceUpdateService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> Handle(ProcessRecurringTransactionsCommand command, CancellationToken ct)
    {
        var dueSchedules = await _scheduleRepository.GetDueSchedulesAsync(ct);
        var processedCount = 0;

        foreach (var schedule in dueSchedules)
        {
            try
            {
                var account = await _accountRepository.GetByIdAsync(schedule.AccountId, ct);
                if (account == null || !account.IsActive) continue;

                var transaction = new Transaction
                {
                    UserId = schedule.UserId,
                    AccountId = schedule.AccountId,
                    CategoryId = schedule.CategoryId,
                    Type = schedule.Type,
                    Amount = schedule.Amount,
                    Description = schedule.Description,
                    TransactionDate = schedule.NextOccurrenceDate ?? DateTime.UtcNow,
                    IsRecurring = true,
                    RecurringScheduleId = schedule.Id,
                    Source = TransactionSource.Manual,
                    IsVerified = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _transactionRepository.AddAsync(transaction, ct);
                await _balanceUpdateService.ApplyTransactionAsync(account, transaction, ct);

                // Update schedule's next occurrence
                schedule.LastProcessedDate = DateTime.UtcNow;
                schedule.NextOccurrenceDate = CalculateNextOccurrence(schedule);
                schedule.UpdatedAt = DateTime.UtcNow;
                await _scheduleRepository.UpdateAsync(schedule);

                processedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing recurring schedule {ScheduleId}", schedule.Id);
            }
        }

        if (processedCount > 0)
            await _unitOfWork.SaveChangesAsync(ct);

        return processedCount;
    }

    private static DateTime? CalculateNextOccurrence(RecurringSchedule schedule)
    {
        var baseDate = schedule.NextOccurrenceDate ?? DateTime.UtcNow;

        var nextDate = schedule.Frequency switch
        {
            RecurringFrequency.Daily => baseDate.AddDays(schedule.IntervalValue),
            RecurringFrequency.Weekly => baseDate.AddDays(7 * schedule.IntervalValue),
            RecurringFrequency.BiWeekly => baseDate.AddDays(14 * schedule.IntervalValue),
            RecurringFrequency.Monthly => baseDate.AddMonths(schedule.IntervalValue),
            RecurringFrequency.Quarterly => baseDate.AddMonths(3 * schedule.IntervalValue),
            RecurringFrequency.SemiAnnually => baseDate.AddMonths(6 * schedule.IntervalValue),
            RecurringFrequency.Annually => baseDate.AddYears(schedule.IntervalValue),
            _ => baseDate.AddMonths(1)
        };

        if (schedule.DayOfMonth.HasValue && schedule.Frequency is RecurringFrequency.Monthly
            or RecurringFrequency.Quarterly or RecurringFrequency.SemiAnnually
            or RecurringFrequency.Annually)
        {
            var day = Math.Min(schedule.DayOfMonth.Value, DateTime.DaysInMonth(nextDate.Year, nextDate.Month));
            nextDate = new DateTime(nextDate.Year, nextDate.Month, day);
        }

        if (schedule.EndDate.HasValue && nextDate > schedule.EndDate.Value)
            return null;

        return nextDate;
    }
}
