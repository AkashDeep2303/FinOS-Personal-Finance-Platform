using FinOS.CoreFinance.Application.DTOs;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;

namespace FinOS.CoreFinance.Application.Queries;

public class GetRecurringSchedulesQuery : IRequest<List<RecurringScheduleDto>>
{
    public long UserId { get; set; }
}

public class GetRecurringSchedulesQueryHandler : IRequestHandler<GetRecurringSchedulesQuery, List<RecurringScheduleDto>>
{
    private readonly IRecurringScheduleRepository _recurringScheduleRepository;

    public GetRecurringSchedulesQueryHandler(IRecurringScheduleRepository recurringScheduleRepository)
    {
        _recurringScheduleRepository = recurringScheduleRepository;
    }

    public async Task<List<RecurringScheduleDto>> Handle(GetRecurringSchedulesQuery query, CancellationToken ct)
    {
        var schedules = await _recurringScheduleRepository.GetByUserIdAsync(query.UserId, ct);

        return schedules.Select(s => new RecurringScheduleDto
        {
            Id = s.Id,
            UserId = s.UserId,
            AccountId = s.AccountId,
            AccountName = s.Account?.Name ?? string.Empty,
            CategoryId = s.CategoryId,
            CategoryName = s.Category?.Name,
            Type = s.Type.ToString(),
            Amount = s.Amount,
            Description = s.Description,
            Frequency = s.Frequency.ToString(),
            IntervalValue = s.IntervalValue,
            DayOfMonth = s.DayOfMonth,
            DayOfWeek = s.DayOfWeek,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            NextOccurrenceDate = s.NextOccurrenceDate,
            LastProcessedDate = s.LastProcessedDate,
            IsActive = s.IsActive,
            AutoCreate = s.AutoCreate,
            CreatedAt = s.CreatedAt
        }).ToList();
    }
}
