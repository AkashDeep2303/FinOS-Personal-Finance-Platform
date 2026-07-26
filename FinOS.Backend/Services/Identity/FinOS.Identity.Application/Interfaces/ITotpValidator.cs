namespace FinOS.Identity.Application.Interfaces;

public interface ITotpValidator
{
    bool Validate(string base32Secret, string code, DateTime utcNow);
}
