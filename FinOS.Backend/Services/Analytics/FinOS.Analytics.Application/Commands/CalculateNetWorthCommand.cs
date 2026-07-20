using FinOS.Common.Interfaces;
using FinOS.Analytics.Application.DTOs;
using FinOS.Analytics.Domain.Entities;
using FinOS.Analytics.Domain.Interfaces;
using MediatR;

namespace FinOS.Analytics.Application.Commands;

public record CalculateNetWorthCommand(CalculateNetWorthDto Dto) : IRequest<NetWorthDto>;

public class CalculateNetWorthCommandHandler : IRequestHandler<CalculateNetWorthCommand, NetWorthDto>
{
    private readonly INetWorthRepository _netWorthRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CalculateNetWorthCommandHandler(INetWorthRepository netWorthRepository, IUnitOfWork unitOfWork)
    {
        _netWorthRepository = netWorthRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<NetWorthDto> Handle(CalculateNetWorthCommand request, CancellationToken ct)
    {
        var dto = request.Dto;
        var totalAssets = dto.CashAndBank + dto.InvestmentValue + dto.RealEstateValue + dto.GoldValue + dto.OtherAssets;
        var totalLiabilities = dto.LoanOutstanding + dto.CreditCardOutstanding + dto.OtherLiabilities;
        var netWorth = totalAssets - totalLiabilities;

        var previous = await _netWorthRepository.GetLatestByUserAsync(dto.UserId, ct);
        decimal? changeFromPrevious = previous != null ? netWorth - previous.NetWorth : null;
        decimal? changePctFromPrevious = previous != null && previous.NetWorth != 0
            ? Math.Round((netWorth - previous.NetWorth) / Math.Abs(previous.NetWorth) * 100, 2) : null;

        var snapshot = new NetWorthSnapshot
        {
            UserId = dto.UserId,
            SnapshotDate = DateTime.UtcNow,
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiabilities,
            NetWorth = netWorth,
            CashAndBank = dto.CashAndBank,
            InvestmentValue = dto.InvestmentValue,
            RealEstateValue = dto.RealEstateValue,
            GoldValue = dto.GoldValue,
            OtherAssets = dto.OtherAssets,
            LoanOutstanding = dto.LoanOutstanding,
            CreditCardOutstanding = dto.CreditCardOutstanding,
            OtherLiabilities = dto.OtherLiabilities,
            ChangeFromPrevious = changeFromPrevious,
            ChangePctFromPrevious = changePctFromPrevious,
            CreatedAt = DateTime.UtcNow
        };

        await _netWorthRepository.AddAsync(snapshot, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new NetWorthDto(
            snapshot.Id, snapshot.UserId, snapshot.SnapshotDate, snapshot.TotalAssets,
            snapshot.TotalLiabilities, snapshot.NetWorth, snapshot.CashAndBank,
            snapshot.InvestmentValue, snapshot.RealEstateValue, snapshot.GoldValue,
            snapshot.OtherAssets, snapshot.LoanOutstanding, snapshot.CreditCardOutstanding,
            snapshot.OtherLiabilities, snapshot.ChangeFromPrevious, snapshot.ChangePctFromPrevious,
            snapshot.CreatedAt
        );
    }
}
