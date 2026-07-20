using FinOS.Investment.Application.DTOs;
using FinOS.Investment.Domain.Entities;
using FinOS.Investment.Domain.Enums;
using FinOS.Investment.Domain.Interfaces;
using FinOS.Common.Exceptions;
using FinOS.Common.Interfaces;
using MediatR;

namespace FinOS.Investment.Application.Commands;

public class RecordInvestmentTransactionCommand : IRequest<InvestmentTransactionDto>
{
    public RecordTransactionRequest Request { get; set; }

    public RecordInvestmentTransactionCommand(RecordTransactionRequest request)
    {
        Request = request;
    }
}

public class RecordInvestmentTransactionCommandHandler : IRequestHandler<RecordInvestmentTransactionCommand, InvestmentTransactionDto>
{
    private readonly IHoldingRepository _holdingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RecordInvestmentTransactionCommandHandler(IHoldingRepository holdingRepository, IUnitOfWork unitOfWork)
    {
        _holdingRepository = holdingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<InvestmentTransactionDto> Handle(RecordInvestmentTransactionCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var holding = await _holdingRepository.GetWithTransactionsAsync(req.HoldingId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Holding), req.HoldingId);

        var transaction = new InvestmentTransaction
        {
            HoldingId = req.HoldingId,
            TransactionType = req.TransactionType,
            Quantity = req.Quantity,
            PricePerUnit = req.PricePerUnit,
            TotalAmount = req.Quantity * req.PricePerUnit,
            Charges = req.Charges,
            STT = req.STT,
            StampDuty = req.StampDuty,
            TransactionDate = req.TransactionDate,
            SettlementDate = req.TransactionDate.AddDays(2),
            SourceAccountId = req.SourceAccountId,
            Notes = req.Notes,
            CreatedAt = DateTime.UtcNow
        };

        holding.InvestmentTransactions.Add(transaction);

        // Update holding based on transaction type
        switch (req.TransactionType)
        {
            case TransactionType.Buy:
                var totalCost = req.Quantity * req.PricePerUnit + req.Charges + req.STT + req.StampDuty;
                var newQuantity = holding.Quantity + req.Quantity;
                holding.AvgPurchasePrice = (holding.InvestedAmount + totalCost) / newQuantity;
                holding.Quantity = newQuantity;
                holding.InvestedAmount += totalCost;
                break;
            case TransactionType.Sell:
                holding.Quantity -= req.Quantity;
                holding.InvestedAmount -= req.Quantity * holding.AvgPurchasePrice;
                if (holding.Quantity <= 0) holding.IsActive = false;
                break;
            case TransactionType.Dividend:
                holding.DividendReceived += req.Quantity * req.PricePerUnit;
                break;
            case TransactionType.SIP:
                var sipCost = req.Quantity * req.PricePerUnit;
                holding.Quantity += req.Quantity;
                holding.AvgPurchasePrice = (holding.InvestedAmount + sipCost) / holding.Quantity;
                holding.InvestedAmount += sipCost;
                break;
        }

        holding.CurrentValue = holding.Quantity * holding.CurrentPrice;
        holding.TotalReturn = holding.CurrentValue - holding.InvestedAmount;
        holding.TotalReturnPct = holding.InvestedAmount > 0
            ? Math.Round((holding.CurrentValue - holding.InvestedAmount) / holding.InvestedAmount * 100, 2)
            : 0;

        await _holdingRepository.UpdateAsync(holding);
        await _unitOfWork.SaveChangesAsync(ct);

        return new InvestmentTransactionDto
        {
            Id = transaction.Id,
            HoldingId = transaction.HoldingId,
            TransactionType = transaction.TransactionType,
            TransactionTypeDisplay = transaction.TransactionType.ToString(),
            Quantity = transaction.Quantity,
            PricePerUnit = transaction.PricePerUnit,
            TotalAmount = transaction.TotalAmount,
            Charges = transaction.Charges,
            STT = transaction.STT,
            StampDuty = transaction.StampDuty,
            TransactionDate = transaction.TransactionDate,
            Notes = transaction.Notes
        };
    }
}
