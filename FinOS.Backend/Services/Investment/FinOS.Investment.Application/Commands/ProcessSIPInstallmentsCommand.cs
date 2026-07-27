using FinOS.Investment.Application.DTOs;
using FinOS.Investment.Domain.Entities;
using FinOS.Investment.Domain.Enums;
using FinOS.Investment.Domain.Interfaces;
using FinOS.Common.Interfaces;
using MediatR;

namespace FinOS.Investment.Application.Commands;

public class ProcessSIPInstallmentsCommand : IRequest<int>
{
    public DateTime AsOfDate { get; set; } = DateTime.UtcNow;
}

public class ProcessSIPInstallmentsCommandHandler : IRequestHandler<ProcessSIPInstallmentsCommand, int>
{
    private readonly ISIPRepository _sipRepository;
    private readonly IHoldingRepository _holdingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProcessSIPInstallmentsCommandHandler(
        ISIPRepository sipRepository,
        IHoldingRepository holdingRepository,
        IUnitOfWork unitOfWork)
    {
        _sipRepository = sipRepository;
        _holdingRepository = holdingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(ProcessSIPInstallmentsCommand command, CancellationToken ct)
    {
        var dueSIPs = await _sipRepository.GetDueSIPsAsync(command.AsOfDate, ct);
        var processedCount = 0;

        foreach (var sip in dueSIPs)
        {
            if (!sip.HoldingId.HasValue) continue;
            var holdingId = sip.HoldingId.Value;
            var holding = await _holdingRepository.GetByIdAsync(holdingId, ct);
            if (holding == null || !holding.IsActive) continue;

            var units = sip.Amount / holding.CurrentPrice;

            var transaction = new InvestmentTransaction
            {
                HoldingId = holdingId,
                TransactionType = TransactionType.SIP,
                Quantity = units,
                PricePerUnit = holding.CurrentPrice,
                TotalAmount = sip.Amount,
                Charges = 0,
                STT = 0,
                StampDuty = 0,
                TransactionDate = command.AsOfDate,
                SettlementDate = command.AsOfDate.AddDays(2),
                SIPId = sip.Id,
                SourceAccountId = sip.SourceAccountId,
                Notes = $"SIP installment - {sip.Frequency}",
                CreatedAt = DateTime.UtcNow
            };

            holding.InvestmentTransactions.Add(transaction);
            holding.Quantity += units;
            holding.AvgPurchasePrice = (holding.InvestedAmount + sip.Amount) / holding.Quantity;
            holding.InvestedAmount += sip.Amount;
            holding.CurrentValue = holding.Quantity * holding.CurrentPrice;

            sip.InstallmentsDone++;
            sip.TotalInvested += sip.Amount;
            sip.LastExecutedDate = command.AsOfDate;
            sip.NextExecutionDate = CalculateNextDate(command.AsOfDate, sip.Frequency, sip.DayOfMonth);

            await _holdingRepository.UpdateAsync(holding);
            processedCount++;
        }

        if (processedCount > 0)
            await _unitOfWork.SaveChangesAsync(ct);

        return processedCount;
    }

    private static DateTime CalculateNextDate(DateTime currentDate, SIPFrequency frequency, int dayOfMonth)
    {
        var next = frequency switch
        {
            SIPFrequency.Weekly => currentDate.AddDays(7),
            SIPFrequency.BiWeekly => currentDate.AddDays(14),
            SIPFrequency.Monthly => currentDate.AddMonths(1),
            SIPFrequency.Quarterly => currentDate.AddMonths(3),
            _ => currentDate.AddMonths(1)
        };

        var adjustedDay = Math.Min(dayOfMonth, DateTime.DaysInMonth(next.Year, next.Month));
        return new DateTime(next.Year, next.Month, adjustedDay);
    }
}
