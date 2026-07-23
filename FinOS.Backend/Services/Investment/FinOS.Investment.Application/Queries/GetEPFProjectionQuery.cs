using FinOS.Investment.Application.DTOs;
using FinOS.Investment.Domain.Interfaces;
using FinOS.Common.Exceptions;
using FinOS.Common.Helpers;
using MediatR;

namespace FinOS.Investment.Application.Queries;

public class GetEPFProjectionQuery : IRequest<EPFProjectionDto>
{
    public long EPFAccountId { get; set; }
    public long UserId { get; set; }
    public int? RetirementAge { get; set; }
    public int? CurrentAge { get; set; }

    public GetEPFProjectionQuery(long epfAccountId, long userId, int? retirementAge = null, int? currentAge = null)
    {
        EPFAccountId = epfAccountId;
        UserId = userId;
        RetirementAge = retirementAge ?? 60;
        CurrentAge = currentAge ?? 30;
    }
}

public class GetEPFProjectionQueryHandler : IRequestHandler<GetEPFProjectionQuery, EPFProjectionDto>
{
    private readonly IEPFAccountRepository _epfRepository;

    public GetEPFProjectionQueryHandler(IEPFAccountRepository epfRepository)
    {
        _epfRepository = epfRepository;
    }

    public async Task<EPFProjectionDto> Handle(GetEPFProjectionQuery query, CancellationToken ct)
    {
        var account = await _epfRepository.GetByIdAsync(query.EPFAccountId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.EPFAccount), query.EPFAccountId);
        if (account.UserId != query.UserId) throw new UnauthorizedAccessException();

        var yearsToRetirement = query.RetirementAge!.Value - query.CurrentAge!.Value;
        var monthlyContribution = account.MonthlySalary * (account.EmployeeContributionPct + account.EmployerContributionPct) / 100;
        var monthlyRate = account.InterestRate / 12 / 100;

        var projectedCorpus = FinancialCalculator.CompoundInterest(
            account.CurrentBalance,
            account.InterestRate,
            yearsToRetirement * 12,
            monthlyContribution);

        var totalContributions = account.CurrentBalance + (monthlyContribution * yearsToRetirement * 12);
        var totalInterest = projectedCorpus - totalContributions;

        var yearlyBreakdown = new List<YearlyProjectionDto>();
        var balance = account.CurrentBalance;
        var yearlyContribution = monthlyContribution * 12;

        for (int year = 1; year <= yearsToRetirement; year++)
        {
            var openingBalance = balance;
            var yearlyInterest = FinancialCalculator.CompoundInterest(balance, account.InterestRate, 12, monthlyContribution) - balance - yearlyContribution;
            balance = openingBalance + yearlyContribution + yearlyInterest;

            yearlyBreakdown.Add(new YearlyProjectionDto
            {
                Year = year,
                OpeningBalance = openingBalance,
                YearlyContribution = yearlyContribution,
                InterestEarned = yearlyInterest,
                ClosingBalance = balance
            });
        }

        return new EPFProjectionDto
        {
            CurrentBalance = account.CurrentBalance,
            MonthlyContribution = monthlyContribution,
            InterestRate = account.InterestRate,
            YearsToRetirement = yearsToRetirement,
            ProjectedCorpus = projectedCorpus,
            TotalContributions = totalContributions,
            TotalInterestEarned = totalInterest,
            YearlyBreakdown = yearlyBreakdown
        };
    }
}
