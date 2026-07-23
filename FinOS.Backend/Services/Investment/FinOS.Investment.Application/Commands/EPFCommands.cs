using FinOS.Common.Exceptions;
using FinOS.Investment.Application.DTOs;
using FinOS.Investment.Domain.Interfaces;
using MediatR;

namespace FinOS.Investment.Application.Commands;

public record CreateEPFAccountCommand(long UserId, CreateEPFAccountRequest Request) : IRequest<EPFTrackerDto>;
public record AddEPFContributionCommand(long UserId, long AccountId, AddEPFContributionRequest Request) : IRequest<EPFContributionDto>;

public class CreateEPFAccountCommandHandler : IRequestHandler<CreateEPFAccountCommand, EPFTrackerDto>
{
    private readonly IEPFAccountRepository _repo;
    public CreateEPFAccountCommandHandler(IEPFAccountRepository repo) => _repo = repo;
    public async Task<EPFTrackerDto> Handle(CreateEPFAccountCommand c, CancellationToken ct)
    {
        var r = c.Request;
        var id = await _repo.CreateAccountAsync(c.UserId, r.UAN, r.EstablishmentCode, r.EmployerName, r.EmployeeContributionPct, r.EmployerContributionPct, r.MonthlySalary, r.CurrentBalance, r.InterestRate, r.StartDate, ct);
        var account = await _repo.GetWithContributionsAsync(id, ct) ?? throw new NotFoundException("EPFAccount", id);
        return EPFMapper.Map(account);
    }
}

public class AddEPFContributionCommandHandler : IRequestHandler<AddEPFContributionCommand, EPFContributionDto>
{
    private readonly IEPFAccountRepository _repo;
    public AddEPFContributionCommandHandler(IEPFAccountRepository repo) => _repo = repo;
    public async Task<EPFContributionDto> Handle(AddEPFContributionCommand c, CancellationToken ct)
    {
        var x = await _repo.AddContributionAsync(c.AccountId, c.UserId, c.Request.Month, c.Request.MonthlySalary, ct);
        return EPFMapper.MapContribution(x);
    }
}

internal static class EPFMapper
{
    public static EPFTrackerDto Map(Domain.Entities.EPFAccount a) => new()
    {
        Id = a.Id, MaskedUAN = Mask(a.UAN), EmployerName = a.EmployerName, EmployeeContributionPct = a.EmployeeContributionPct,
        EmployerContributionPct = a.EmployerContributionPct, EPSCorpus = a.EPSCorpus, CurrentBalance = a.CurrentBalance,
        MonthlySalary = a.MonthlySalary, InterestRate = a.InterestRate, StartDate = a.StartDate, IsActive = a.IsActive,
        EmployeeContribution = a.Contributions.Sum(x => x.EmployeeContribution), EmployerContribution = a.Contributions.Sum(x => x.EmployerContribution),
        EPSContribution = a.Contributions.Sum(x => x.EPSContribution), InterestEarned = a.Contributions.Sum(x => x.InterestEarned),
        Contributions = a.Contributions.OrderByDescending(x => x.Month).Select(MapContribution).ToList()
    };
    public static EPFContributionDto MapContribution(Domain.Entities.EPFContribution x) => new()
    {
        Id = x.Id, Month = x.Month, EmployeeContribution = x.EmployeeContribution, EmployerContribution = x.EmployerContribution,
        EPSContribution = x.EPSContribution, InterestEarned = x.InterestEarned, OpeningBalance = x.OpeningBalance, ClosingBalance = x.ClosingBalance
    };
    private static string? Mask(string? uan) => string.IsNullOrWhiteSpace(uan) ? null : "XXXX-XXXX-" + uan[^Math.Min(4, uan.Length)..];
}
