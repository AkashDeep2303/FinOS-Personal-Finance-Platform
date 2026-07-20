using FinOS.Investment.Application.DTOs;
using FinOS.Investment.Domain.Interfaces;
using FinOS.Common.Exceptions;
using FinOS.Common.Interfaces;
using MediatR;

namespace FinOS.Investment.Application.Commands;

public class CreateHoldingCommand : IRequest<HoldingDto>
{
    public CreateHoldingRequest Request { get; set; }

    public CreateHoldingCommand(CreateHoldingRequest request)
    {
        Request = request;
    }
}

public class CreateHoldingCommandHandler : IRequestHandler<CreateHoldingCommand, HoldingDto>
{
    private readonly IHoldingRepository _holdingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateHoldingCommandHandler(IHoldingRepository holdingRepository, IUnitOfWork unitOfWork)
    {
        _holdingRepository = holdingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<HoldingDto> Handle(CreateHoldingCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var invested = req.Quantity * req.AvgPurchasePrice;
        var currentValue = req.Quantity * req.CurrentPrice;

        var holding = new Domain.Entities.Holding
        {
            PortfolioId = req.PortfolioId,
            InvestmentTypeId = req.InvestmentTypeId,
            Symbol = req.Symbol,
            Name = req.Name,
            Quantity = req.Quantity,
            AvgPurchasePrice = req.AvgPurchasePrice,
            CurrentPrice = req.CurrentPrice,
            CurrentValue = currentValue,
            InvestedAmount = invested,
            DayChange = 0,
            DayChangePct = 0,
            TotalReturn = currentValue - invested,
            TotalReturnPct = invested > 0 ? Math.Round((currentValue - invested) / invested * 100, 2) : 0,
            FundHouse = req.FundHouse,
            FundCategory = req.FundCategory,
            RiskLevel = req.RiskLevel,
            MaturityDate = req.MaturityDate,
            InterestRate = req.InterestRate,
            LockInEndDate = req.LockInEndDate,
            NAVDate = DateTime.UtcNow,
            LastPriceUpdateAt = DateTime.UtcNow,
            Notes = req.Notes,
            IsActive = true
        };

        await _holdingRepository.AddAsync(holding, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToDto(holding);
    }

    private static HoldingDto MapToDto(Domain.Entities.Holding h) => new()
    {
        Id = h.Id, PortfolioId = h.PortfolioId, InvestmentTypeId = h.InvestmentTypeId,
        Symbol = h.Symbol, Name = h.Name, Quantity = h.Quantity,
        AvgPurchasePrice = h.AvgPurchasePrice, CurrentPrice = h.CurrentPrice,
        CurrentValue = h.CurrentValue, InvestedAmount = h.InvestedAmount,
        DayChange = h.DayChange, DayChangePct = h.DayChangePct,
        TotalReturn = h.TotalReturn, TotalReturnPct = h.TotalReturnPct,
        XIRR = h.XIRR, CAGR = h.CAGR, DividendReceived = h.DividendReceived,
        FundHouse = h.FundHouse, FundCategory = h.FundCategory, RiskLevel = h.RiskLevel,
        MaturityDate = h.MaturityDate, InterestRate = h.InterestRate,
        LockInEndDate = h.LockInEndDate, IsActive = h.IsActive
    };
}
