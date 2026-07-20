using FinOS.Investment.Application.DTOs;
using FinOS.Investment.Domain.Interfaces;
using FinOS.Common.Exceptions;
using FinOS.Common.Interfaces;
using MediatR;

namespace FinOS.Investment.Application.Commands;

public class UpdateEPFContributionCommand : IRequest<EPFContributionDto>
{
    public UpdateEPFContributionRequest Request { get; set; }

    public UpdateEPFContributionCommand(UpdateEPFContributionRequest request)
    {
        Request = request;
    }
}

public class UpdateEPFContributionCommandHandler : IRequestHandler<UpdateEPFContributionCommand, EPFContributionDto>
{
    private readonly IEPFAccountRepository _epfRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEPFContributionCommandHandler(IEPFAccountRepository epfRepository, IUnitOfWork unitOfWork)
    {
        _epfRepository = epfRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<EPFContributionDto> Handle(UpdateEPFContributionCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var epfAccount = await _epfRepository.GetWithContributionsAsync(req.EPFAccountId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.EPFAccount), req.EPFAccountId);

        var employeeContribution = Math.Round(req.MonthlySalary * epfAccount.EmployeeContributionPct / 100, 2);
        var employerContribution = Math.Round(req.MonthlySalary * epfAccount.EmployerContributionPct / 100, 2);
        var epsContribution = Math.Round(Math.Min(req.MonthlySalary * 8.33m / 100, 1250m), 2);
        var epfEmployerPart = employerContribution - epsContribution;

        var lastContribution = epfAccount.Contributions
            .OrderByDescending(c => c.Month)
            .FirstOrDefault();

        var openingBalance = lastContribution?.ClosingBalance ?? epfAccount.CurrentBalance;
        var totalContribution = employeeContribution + epfEmployerPart;
        var monthlyInterestRate = epfAccount.InterestRate / 12 / 100;
        var interestEarned = Math.Round((openingBalance + totalContribution) * monthlyInterestRate, 2);
        var closingBalance = openingBalance + totalContribution + interestEarned;

        var contribution = new Domain.Entities.EPFContribution
        {
            EPFAccountId = req.EPFAccountId,
            Month = req.Month,
            EmployeeContribution = employeeContribution,
            EmployerContribution = employerContribution,
            EPSContribution = epsContribution,
            InterestEarned = interestEarned,
            OpeningBalance = openingBalance,
            ClosingBalance = closingBalance,
            CreatedAt = DateTime.UtcNow
        };

        epfAccount.Contributions.Add(contribution);
        epfAccount.CurrentBalance = closingBalance;
        epfAccount.EPSCorpus += epsContribution;
        epfAccount.MonthlySalary = req.MonthlySalary;

        await _epfRepository.UpdateAsync(epfAccount);
        await _unitOfWork.SaveChangesAsync(ct);

        return new EPFContributionDto
        {
            Id = contribution.Id,
            Month = contribution.Month,
            EmployeeContribution = contribution.EmployeeContribution,
            EmployerContribution = contribution.EmployerContribution,
            EPSContribution = contribution.EPSContribution,
            InterestEarned = contribution.InterestEarned,
            OpeningBalance = contribution.OpeningBalance,
            ClosingBalance = contribution.ClosingBalance
        };
    }
}
