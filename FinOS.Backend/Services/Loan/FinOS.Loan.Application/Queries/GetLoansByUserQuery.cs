using FinOS.Loan.Application.DTOs;
using FinOS.Loan.Domain.Interfaces;
using MediatR;

namespace FinOS.Loan.Application.Queries;

public class GetLoansByUserQuery : IRequest<List<LoanListDto>>
{
    public long UserId { get; set; }
    public bool? IsActive { get; set; }

    public GetLoansByUserQuery(long userId, bool? isActive = null)
    {
        UserId = userId;
        IsActive = isActive;
    }
}

public class GetLoansByUserQueryHandler : IRequestHandler<GetLoansByUserQuery, List<LoanListDto>>
{
    private readonly ILoanRepository _loanRepository;

    public GetLoansByUserQueryHandler(ILoanRepository loanRepository)
    {
        _loanRepository = loanRepository;
    }

    public async Task<List<LoanListDto>> Handle(GetLoansByUserQuery query, CancellationToken ct)
    {
        var loans = query.IsActive == true
            ? await _loanRepository.GetActiveByUserIdAsync(query.UserId, ct)
            : await _loanRepository.GetByUserIdAsync(query.UserId, ct);

        return loans.Select(l => new LoanListDto
        {
            Id = l.Id,
            LoanTypeName = l.LoanType?.Name ?? string.Empty,
            LenderName = l.LenderName,
            PrincipalAmount = l.PrincipalAmount,
            OutstandingPrincipal = l.OutstandingPrincipal,
            EMI = l.EMI,
            InterestRate = l.InterestRate,
            RemainingTenureMonths = l.RemainingTenureMonths,
            Status = l.Status,
            NextEMIDate = l.NextEMIDate
        }).ToList();
    }
}
