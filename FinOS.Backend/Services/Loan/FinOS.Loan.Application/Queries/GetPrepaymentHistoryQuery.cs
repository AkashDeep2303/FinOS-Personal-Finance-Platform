using FinOS.Common.Exceptions;
using FinOS.Loan.Application.DTOs;
using FinOS.Loan.Domain.Interfaces;
using MediatR;

namespace FinOS.Loan.Application.Queries;

public class GetPrepaymentHistoryQuery : IRequest<List<LoanPrepaymentDto>>
{
    public long LoanId { get; set; }

    public GetPrepaymentHistoryQuery(long loanId) { LoanId = loanId; }
}

public class GetPrepaymentHistoryQueryHandler : IRequestHandler<GetPrepaymentHistoryQuery, List<LoanPrepaymentDto>>
{
    private readonly ILoanPrepaymentRepository _prepaymentRepository;

    public GetPrepaymentHistoryQueryHandler(ILoanPrepaymentRepository prepaymentRepository)
    {
        _prepaymentRepository = prepaymentRepository;
    }

    public async Task<List<LoanPrepaymentDto>> Handle(GetPrepaymentHistoryQuery query, CancellationToken ct)
    {
        var prepayments = await _prepaymentRepository.GetByLoanIdAsync(query.LoanId, ct);

        return prepayments.OrderByDescending(p => p.PrepaymentDate).Select(p => new LoanPrepaymentDto
        {
            Id = p.Id, LoanId = p.LoanId,
            PrepaymentDate = p.PrepaymentDate,
            PrepaymentAmount = p.PrepaymentAmount,
            PenaltyAmount = p.PenaltyAmount,
            PrepaymentType = p.PrepaymentType,
            TenureReduction = p.TenureReduction,
            InterestSaved = p.InterestSaved,
            NewOutstanding = p.NewOutstanding,
            NewEMI = p.NewEMI,
            NewTenureMonths = p.NewTenureMonths
        }).ToList();
    }
}
