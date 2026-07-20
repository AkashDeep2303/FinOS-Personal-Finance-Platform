using FinOS.Identity.Application.Interfaces;

namespace FinOS.Identity.Application.Services;

public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch
        {
            return false;
        }
    }

    public string GenerateSalt()
    {
        return BCrypt.Net.BCrypt.GenerateSalt(workFactor: 12);
    }
}
