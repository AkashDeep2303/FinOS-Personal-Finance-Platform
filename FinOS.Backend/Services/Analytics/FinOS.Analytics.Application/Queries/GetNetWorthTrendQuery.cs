using FinOS.Analytics.Application.DTOs;
using FinOS.Analytics.Domain.Interfaces;
using MediatR;

namespace FinOS.Analytics.Application.Queries;

public record GetNetWorthTrendQuery(long UserId, int Months = 12) : IRequest<List<NetWorthDto>>;

public class GetNetWorthTrendQueryHandler : IRequestHandler<GetNetWorthTrendQuery, List<NetWorthDto>>
{
    private readonly INetWorthRepository _netWorthRepository;

    public GetNetWorthTrendQueryHandler(INetWorthRepository netWorthRepository)
    {
        _netWorthRepository = netWorthRepository;
    }

    public async Task<List<NetWorthDto>> Handle(GetNetWorthTrendQuery request, CancellationToken ct)
    {
        var snapshots = await _netWorthRepository.GetByUserAsync(request.UserId, request.Months, ct);

        return snapshots.Select(s => new NetWorthDto(
            s.Id, s.UserId, s.SnapshotDate, s.TotalAssets, s.TotalLiabilities,
            s.NetWorth, s.CashAndBank, s.InvestmentValue, s.RealEstateValue,
            s.GoldValue, s.OtherAssets, s.LoanOutstanding, s.CreditCardOutstanding,
            s.OtherLiabilities, s.ChangeFromPrevious, s.ChangePctFromPrevious, s.CreatedAt
        )).OrderBy(s => s.SnapshotDate).ToList();
    }
}
