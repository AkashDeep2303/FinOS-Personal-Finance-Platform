using FinOS.Identity.Application.Interfaces;
using FinOS.Common.Helpers;

namespace FinOS.Identity.Application.Services;

public sealed class TotpValidator : ITotpValidator
{
    public bool Validate(string base32Secret, string code, DateTime utcNow) =>
        TotpCodeValidator.Validate(base32Secret, code, utcNow);
}
