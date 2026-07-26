using FinOS.Common.Exceptions;
using FinOS.Loan.Domain.Entities;
using FinOS.Loan.Domain.Interfaces;

namespace FinOS.Loan.Application.Services;

internal static class LoanOwnership
{
    internal static async Task<Domain.Entities.Loan> GetOwnedAsync(
        ILoanRepository repository, long loanId, long userId, CancellationToken cancellationToken)
    {
        var loan = await repository.GetByIdAsync(loanId, cancellationToken);
        if (loan is null || loan.UserId != userId)
            throw new NotFoundException(nameof(Domain.Entities.Loan), loanId);
        return loan;
    }

    internal static Domain.Entities.Loan EnsureOwned(
        Domain.Entities.Loan? loan, long loanId, long userId)
    {
        if (loan is null || loan.UserId != userId)
            throw new NotFoundException(nameof(Domain.Entities.Loan), loanId);
        return loan;
    }
}
