using FinOS.CoreFinance.Application.DTOs;
using FinOS.CoreFinance.Application.Services;
using FinOS.CoreFinance.Domain.Entities;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;
using System.Text.Json;

namespace FinOS.CoreFinance.Application.Commands;

public sealed record CalculateTaxComparisonCommand(long UserId, string FinancialYear)
    : IRequest<TaxRegimeComparisonDto>;

public sealed class CalculateTaxComparisonHandler(ITaxRepository repository)
    : IRequestHandler<CalculateTaxComparisonCommand, TaxRegimeComparisonDto>
{
    public async Task<TaxRegimeComparisonDto> Handle(
        CalculateTaxComparisonCommand request, CancellationToken cancellationToken)
    {
        var profile = await repository.GetProfileAsync(request.UserId, request.FinancialYear, cancellationToken)
            ?? throw new InvalidOperationException("Save a tax profile before calculating a projection.");
        var oldResult = await Calculate("Old");
        var newResult = await Calculate("New");
        string? lower = oldResult.Available && newResult.Available
            ? oldResult.EstimatedTax <= newResult.EstimatedTax ? "Old" : "New"
            : null;
        return new(request.FinancialYear, oldResult, newResult, lower,
            lower is null
                ? "Publish both regime rule versions to enable comparison."
                : $"{lower} has the lower deterministic estimate for the recorded inputs. This is a calculation comparison, not tax advice.");

        async Task<TaxProjectionBreakdownDto> Calculate(string regime)
        {
            var rule = await repository.GetPublishedRuleAsync(request.FinancialYear, regime, cancellationToken);
            if (rule is null)
                return new(regime, false, null, null, 0, 0, 0, 0, 0, 0, 0, 0,
                    [$"No active published {regime} regime rule exists for {request.FinancialYear}."]);
            var result = TaxProjectionCalculator.Calculate(profile.InputJson, rule.ConfigurationJson);
            var evidence = JsonSerializer.Serialize(new
            {
                regime, ruleVersionId = rule.Id, rule.Version,
                result.GrossIncome, result.TaxableIncome, result.BaseTax,
                result.Rebate, result.Cess, result.EstimatedTax, result.TaxesPaid,
                result.EstimatedPayableOrRefund, result.Warnings
            });
            await repository.AddProjectionAsync(new TaxProjection
            {
                UserId = request.UserId, TaxProfileId = profile.Id, TaxRuleVersionId = rule.Id,
                GrossIncome = result.GrossIncome, TaxableIncome = result.TaxableIncome,
                EstimatedTax = result.EstimatedTax, TaxesPaid = result.TaxesPaid,
                EstimatedPayableOrRefund = result.EstimatedPayableOrRefund,
                CalculationJson = evidence
            }, cancellationToken);
            return new(regime, true, rule.Id, rule.Version, result.GrossIncome,
                result.TaxableIncome, result.BaseTax, result.Rebate, result.Cess,
                result.EstimatedTax, result.TaxesPaid, result.EstimatedPayableOrRefund,
                result.Warnings);
        }
    }
}
