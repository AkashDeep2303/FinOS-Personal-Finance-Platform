using FinOS.Common.Exceptions;
using FinOS.Common.Interfaces;
using FinOS.CoreFinance.Application.DTOs;
using FinOS.CoreFinance.Domain.Entities;
using FinOS.CoreFinance.Domain.Enums;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;

namespace FinOS.CoreFinance.Application.Commands;

public class CreateRecurringScheduleCommand : IRequest<RecurringScheduleDto>
{
    public long UserId { get; set; }
    public CreateRecurringScheduleRequest Request { get; set; } = new();
}

public class CreateRecurringScheduleCommandHandler : IRequestHandler<CreateRecurringScheduleCommand, RecurringScheduleDto>
{
    private readonly IRecurringScheduleRepository _recurringScheduleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRecurringScheduleCommandHandler(
        IRecurringScheduleRepository recurringScheduleRepository,
        IUnitOfWork unitOfWork)
    {
        _recurringScheduleRepository = recurringScheduleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RecurringScheduleDto> Handle(CreateRecurringScheduleCommand command, CancellationToken ct)
    {
        if (!Enum.TryParse<TransactionType>(command.Request.Type, true, out var transactionType))
            throw new DomainException("INVALID_TYPE", $"Invalid transaction type: {command.Request.Type}");

        if (!Enum.TryParse<RecurringFrequency>(command.Request.Frequency, true, out var frequency))
            throw new DomainException("INVALID_FREQUENCY", $"Invalid frequency: {command.Request.Frequency}");

        var schedule = new RecurringSchedule
        {
            UserId = command.UserId,
            AccountId = command.Request.AccountId,
            CategoryId = command.Request.CategoryId,
            Type = transactionType,
            Amount = command.Request.Amount,
            Description = command.Request.Description,
            Frequency = frequency,
            IntervalValue = command.Request.IntervalValue,
            DayOfMonth = command.Request.DayOfMonth,
            DayOfWeek = command.Request.DayOfWeek,
            StartDate = command.Request.StartDate,
            EndDate = command.Request.EndDate,
            NextOccurrenceDate = command.Request.StartDate,
            AutoCreate = command.Request.AutoCreate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _recurringScheduleRepository.AddAsync(schedule, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new RecurringScheduleDto
        {
            Id = schedule.Id,
            UserId = schedule.UserId,
            AccountId = schedule.AccountId,
            CategoryId = schedule.CategoryId,
            Type = schedule.Type.ToString(),
            Amount = schedule.Amount,
            Description = schedule.Description,
            Frequency = schedule.Frequency.ToString(),
            IntervalValue = schedule.IntervalValue,
            DayOfMonth = schedule.DayOfMonth,
            DayOfWeek = schedule.DayOfWeek,
            StartDate = schedule.StartDate,
            EndDate = schedule.EndDate,
            NextOccurrenceDate = schedule.NextOccurrenceDate,
            LastProcessedDate = schedule.LastProcessedDate,
            IsActive = schedule.IsActive,
            AutoCreate = schedule.AutoCreate,
            CreatedAt = schedule.CreatedAt
        };
    }
}
