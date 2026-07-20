using FinOS.Investment.Application.DTOs;
using FinOS.Investment.Domain.Interfaces;
using FinOS.Common.Exceptions;
using FinOS.Common.Interfaces;
using MediatR;

namespace FinOS.Investment.Application.Commands;

public class UpdateHoldingPriceCommand : IRequest<HoldingDto>
{
    public long HoldingId { get; set; }
    public UpdateHoldingPriceRequest Request { get; set; }

    public UpdateHoldingPriceCommand(long holdingId, UpdateHoldingPriceRequest request)
    {
        HoldingId = holdingId;
        Request = request;
    }
}

public class UpdateHoldingPriceCommandHandler : IRequestHandler<UpdateHoldingPriceCommand, HoldingDto>
{
    private readonly IHoldingRepository _holdingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateHoldingPriceCommandHandler(IHoldingRepository holdingRepository, IUnitOfWork unitOfWork)
    {
        _holdingRepository = holdingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<HoldingDto> Handle(UpdateHoldingPriceCommand command, CancellationToken ct)
    {
        var holding = await _holdingRepository.GetByIdAsync(command.HoldingId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Holding), command.HoldingId);

        var oldPrice = holding.CurrentPrice;
        var newPrice = command.Request.CurrentPrice;

        holding.CurrentPrice = newPrice;
        holding.CurrentValue = holding.Quantity * newPrice;
        holding.DayChange = holding.Quantity * (newPrice - oldPrice);
        holding.DayChangePct = oldPrice > 0 ? Math.Round((newPrice - oldPrice) / oldPrice * 100, 2) : 0;
        holding.TotalReturn = holding.CurrentValue - holding.InvestedAmount;
        holding.TotalReturnPct = holding.InvestedAmount > 0
            ? Math.Round((holding.CurrentValue - holding.InvestedAmount) / holding.InvestedAmount * 100, 2)
            : 0;
        holding.NAVDate = command.Request.NAVDate ?? DateTime.UtcNow;
        holding.LastPriceUpdateAt = DateTime.UtcNow;

        await _holdingRepository.UpdateAsync(holding);
        await _unitOfWork.SaveChangesAsync(ct);

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
