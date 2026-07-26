using FinOS.Common.Helpers;
using Xunit;

namespace FinOS.Common.Tests;

public sealed class TotpValidatorTests
{
    private const string RfcSecret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";

    [Fact]
    public void Validate_AcceptsRfcCodeAndAdjacentClockWindow()
    {
        var instant = DateTimeOffset.FromUnixTimeSeconds(59).UtcDateTime;

        Assert.True(TotpCodeValidator.Validate(RfcSecret, "287082", instant));
        Assert.True(TotpCodeValidator.Validate(RfcSecret, "287082", instant.AddSeconds(30)));
    }

    [Theory]
    [InlineData("287081")]
    [InlineData("abcdef")]
    [InlineData("12345")]
    public void Validate_RejectsIncorrectOrMalformedCode(string code)
    {
        Assert.False(TotpCodeValidator.Validate(RfcSecret, code,
            DateTimeOffset.FromUnixTimeSeconds(59).UtcDateTime));
    }

    [Fact]
    public void Validate_RejectsMissingOrMalformedSecret()
    {
        var now = DateTime.UtcNow;
        Assert.False(TotpCodeValidator.Validate("", "123456", now));
        Assert.False(TotpCodeValidator.Validate("not-base32!", "123456", now));
    }
}
