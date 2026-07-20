using FinOS.Investment.Application.DTOs;
using FinOS.Investment.Domain.Interfaces;
using MediatR;

namespace FinOS.Investment.Application.Queries;

public class GetSIPListQuery : IRequest<List<SIPDto>>
{
    public long UserId { get; set; }
    public bool? IsActive { get; set; }

    public GetSIPListQuery(long userId, bool? isActive = null)
    {
        UserId = userId;
        IsActive = isActive;
    }
}

public class GetSIPListQueryHandler : IRequestHandler<GetSIPListQuery, List<SIPDto>>
{
    private readonly ISIPRepository _sipRepository;

    public GetSIPListQueryHandler(ISIPRepository sipRepository)
    {
        _sipRepository = sipRepository;
    }

    public async Task<List<SIPDto>> Handle(GetSIPListQuery query, CancellationToken ct)
    {
        var sips = await _sipRepository.GetByUserIdAsync(query.UserId, ct);

        if (query.IsActive == true)
            sips = sips.Where(s => s.IsActive).ToList();

        return sips.Select(s => new SIPDto
        {
            Id = s.Id, UserId = s.UserId, HoldingId = s.HoldingId,
            HoldingName = s.Holding?.Name ?? "", Amount = s.Amount,
            Frequency = s.Frequency, DayOfMonth = s.DayOfMonth,
            StartDate = s.StartDate, EndDate = s.EndDate,
            NextExecutionDate = s.NextExecutionDate, IsActive = s.IsActive,
            TotalInvested = s.TotalInvested, InstallmentsDone = s.InstallmentsDone
        }).ToList();
    }
}
