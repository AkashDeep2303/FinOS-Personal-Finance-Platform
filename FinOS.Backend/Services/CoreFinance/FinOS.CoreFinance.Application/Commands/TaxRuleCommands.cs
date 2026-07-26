using FinOS.Common.Exceptions;
using FinOS.CoreFinance.Domain.Entities;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;

namespace FinOS.CoreFinance.Application.Commands;

public sealed record CreateTaxRuleVersionCommand(
    string FinancialYear,
    string AssessmentYear,
    string Regime,
    string Version,
    string ConfigurationJson,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo) : IRequest<TaxRuleVersion>;

public sealed record PublishTaxRuleVersionCommand(long Id) : IRequest<TaxRuleVersion>;

public sealed class CreateTaxRuleVersionHandler(ITaxRepository repository)
    : IRequestHandler<CreateTaxRuleVersionCommand, TaxRuleVersion>
{
    public Task<TaxRuleVersion> Handle(CreateTaxRuleVersionCommand request, CancellationToken cancellationToken) =>
        repository.CreateRuleVersionAsync(new TaxRuleVersion
        {
            FinancialYear = request.FinancialYear,
            AssessmentYear = request.AssessmentYear,
            Regime = request.Regime,
            Version = request.Version,
            ConfigurationJson = request.ConfigurationJson,
            EffectiveFrom = request.EffectiveFrom.Date,
            EffectiveTo = request.EffectiveTo?.Date
        }, cancellationToken);
}

public sealed class PublishTaxRuleVersionHandler(ITaxRepository repository)
    : IRequestHandler<PublishTaxRuleVersionCommand, TaxRuleVersion>
{
    public async Task<TaxRuleVersion> Handle(PublishTaxRuleVersionCommand request, CancellationToken cancellationToken) =>
        await repository.PublishRuleVersionAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("TaxRuleVersion", request.Id);
}
