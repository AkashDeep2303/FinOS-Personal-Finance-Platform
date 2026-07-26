using System.Buffers.Binary;
using System.Security.Cryptography;

namespace FinOS.Common.Helpers;

public static class TotpCodeValidator
{
    private const int StepSeconds = 30;

    public static bool Validate(string base32Secret, string code, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(base32Secret) ||
            code.Length != 6 || code.Any(c => c is < '0' or > '9'))
            return false;
        byte[] secret;
        try { secret = DecodeBase32(base32Secret); }
        catch (FormatException) { return false; }
        if (secret.Length < 10) return false;

        var counter = new DateTimeOffset(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc))
            .ToUnixTimeSeconds() / StepSeconds;
        for (var offset = -1; offset <= 1; offset++)
        {
            var expected = GenerateCode(secret, counter + offset);
            if (CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.ASCII.GetBytes(expected),
                System.Text.Encoding.ASCII.GetBytes(code)))
                return true;
        }
        return false;
    }

    private static string GenerateCode(byte[] secret, long counter)
    {
        Span<byte> message = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(message, counter);
        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(message.ToArray());
        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24) |
                     (hash[offset + 1] << 16) |
                     (hash[offset + 2] << 8) |
                     hash[offset + 3];
        return (binary % 1_000_000).ToString("D6");
    }

    private static byte[] DecodeBase32(string value)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var normalized = new string(value.Where(c => !char.IsWhiteSpace(c) && c != '-')
            .Select(char.ToUpperInvariant).TakeWhile(c => c != '=').ToArray());
        if (normalized.Length == 0) throw new FormatException();
        var output = new List<byte>(normalized.Length * 5 / 8);
        var buffer = 0;
        var bits = 0;
        foreach (var character in normalized)
        {
            var index = alphabet.IndexOf(character);
            if (index < 0) throw new FormatException();
            buffer = (buffer << 5) | index;
            bits += 5;
            if (bits < 8) continue;
            bits -= 8;
            output.Add((byte)(buffer >> bits));
            buffer &= (1 << bits) - 1;
        }
        return output.ToArray();
    }
}
