using FinOS.Common.Interfaces;
using FinOS.Analytics.Application.DTOs;
using FinOS.Analytics.Domain.Entities;
using FinOS.Analytics.Domain.Interfaces;
using FinOS.Common.Exceptions;
using MediatR;
using System.Text.Json;

namespace FinOS.Analytics.Application.Commands;

public record GenerateMonthlyAggregatesCommand(GenerateMonthlyAggregatesDto Dto) : IRequest<MonthlyAggregateDto>;

public class GenerateMonthlyAggregatesCommandHandler : IRequestHandler<GenerateMonthlyAggregatesCommand, MonthlyAggregateDto>
{
    private readonly IMonthlyAggregateRepository _aggregateRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GenerateMonthlyAggregatesCommandHandler(IMonthlyAggregateRepository aggregateRepository, IUnitOfWork unitOfWork)
    {
        _aggregateRepository = aggregateRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MonthlyAggregateDto> Handle(GenerateMonthlyAggregatesCommand request, CancellationToken ct)
    {
        var dto = request.Dto;

        // Check if aggregate already exists
        var existing = await _aggregateRepository.GetByUserAndMonthAsync(dto.UserId, dto.YearMonth, ct);
        if (existing is not null)
        {
            await _aggregateRepository.RemoveAsync(existing);
        }

        // In production, this would calculate from transaction data
        // For now, create placeholder that will be enriched by real data
        var aggregate = new MonthlyAggregate
        {
            UserId = dto.UserId,
            YearMonth = dto.YearMonth,
            TotalIncome = 0,
            TotalExpense = 0,
            TotalSavings = 0,
            SavingsRate = 0,
            TopExpenseCategory = null,
            TopExpenseAmount = 0,
            TransactionCount = 0,
            CategoryBreakdown = JsonSerializer.Serialize(new List<CategoryBreakdownItem>()),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (existing is null)
            await _aggregateRepository.AddAsync(aggregate, ct);
        else
            await _aggregateRepository.UpdateAsync(aggregate);

        await _unitOfWork.SaveChangesAsync(ct);

        return new MonthlyAggregateDto(
            aggregate.Id, aggregate.UserId, aggregate.YearMonth, aggregate.TotalIncome,
            aggregate.TotalExpense, aggregate.TotalSavings, aggregate.SavingsRate,
            aggregate.TopExpenseCategory, aggregate.TopExpenseAmount, aggregate.TransactionCount,
            aggregate.CategoryBreakdown, aggregate.CreatedAt
        );
    }
}

internal record CategoryBreakdownItem(string Category, decimal Amount, decimal Percentage);
