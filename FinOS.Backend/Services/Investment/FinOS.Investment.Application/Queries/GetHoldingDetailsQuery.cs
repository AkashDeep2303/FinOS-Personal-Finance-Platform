using FinOS.Investment.Application.DTOs;
using FinOS.Investment.Domain.Interfaces;
using FinOS.Common.Exceptions;
using MediatR;

namespace FinOS.Investment.Application.Queries;

public class GetHoldingDetailsQuery : IRequest<HoldingDto>
{
    public long HoldingId { get; set; }

    public GetHoldingDetailsQuery(long holdingId)
    {
        HoldingId = holdingId;
    }
}

public class GetHoldingDetailsQueryHandler : IRequestHandler<GetHoldingDetailsQuery, HoldingDto>
{
    private readonly IHoldingRepository _holdingRepository;

    public GetHoldingDetailsQueryHandler(IHoldingRepository holdingRepository)
    {
        _holdingRepository = holdingRepository;
    }

    public async Task<HoldingDto> Handle(GetHoldingDetailsQuery query, CancellationToken ct)
    {
        var holding = await _holdingRepository.GetWithTransactionsAsync(query.HoldingId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Holding), query.HoldingId);

        return new HoldingDto
        {
            Id = holding.Id, PortfolioId = holding.PortfolioId, InvestmentTypeId = holding.InvestmentTypeId,
            Symbol = holding.Symbol, Name = holding.Name, Quantity = holding.Quantity,
            AvgPurchasePrice = holding.AvgPurchasePrice, CurrentPrice = holding.CurrentPrice,
            CurrentValue = holding.CurrentValue, InvestedAmount = holding.InvestedAmount,
            DayChange = holding.DayChange, DayChangePct = holding.DayChangePct,
            TotalReturn = holding.TotalReturn, TotalReturnPct = holding.TotalReturnPct,
            XIRR = holding.XIRR, CAGR = holding.CAGR, DividendReceived = holding.DividendReceived,
            FundHouse = holding.FundHouse, FundCategory = holding.FundCategory, RiskLevel = holding.RiskLevel,
            MaturityDate = holding.MaturityDate, InterestRate = holding.InterestRate,
            LockInEndDate = holding.LockInEndDate, IsActive = holding.IsActive
        };
    }
}
