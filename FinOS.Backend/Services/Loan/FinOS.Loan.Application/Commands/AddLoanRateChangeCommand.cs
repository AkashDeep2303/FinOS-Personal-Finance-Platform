using FinOS.Common.Exceptions;
using FinOS.Loan.Application.DTOs;
using FinOS.Loan.Domain.Interfaces;
using MediatR;
using FinOS.Loan.Application.Services;

namespace FinOS.Loan.Application.Commands;

public record AddLoanRateChangeCommand(long UserId, long LoanId, AddLoanRateChangeRequest Request) : IRequest<Unit>;
public class AddLoanRateChangeCommandHandler : IRequestHandler<AddLoanRateChangeCommand, Unit>
{
    private readonly ILoanRepository _repository;
    public AddLoanRateChangeCommandHandler(ILoanRepository repository) => _repository = repository;
    public async Task<Unit> Handle(AddLoanRateChangeCommand command, CancellationToken ct)
    {
        await LoanOwnership.GetOwnedAsync(_repository, command.LoanId, command.UserId, ct);
        await _repository.AddRateChangeAsync(command.LoanId, command.Request.NewRate, command.Request.EffectiveDate, command.Request.Reason, ct);
        return Unit.Value;
    }
}
