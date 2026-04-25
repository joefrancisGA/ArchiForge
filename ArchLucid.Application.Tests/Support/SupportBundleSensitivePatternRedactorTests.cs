using ArchLucid.Application.Support;

using FluentAssertions;

namespace ArchLucid.Application.Tests.Support;

[Trait("Category", "Unit")]
public sealed class SupportBundleSensitivePatternRedactorTests
{
    [Fact]
    public void RedactSensitivePatterns_ReplacesBearerToken()
    {
        const string input = "Authorization: Bearer abc.def.ghi-secret";
        string redacted = SupportBundleSensitivePatternRedactor.RedactSensitivePatterns(input);

        redacted.Should().Be("Authorization: Bearer [REDACTED]");
    }

    [Fact]
    public void RedactSensitivePatterns_ReplacesXApiKeyHeader()
    {
        const string input = "X-Api-Key: super-secret-key-value";
        string redacted = SupportBundleSensitivePatternRedactor.RedactSensitivePatterns(input);

        redacted.Should().Be("X-Api-Key: [REDACTED]");
    }

    [Fact]
    public void RedactSensitivePatterns_ReplacesPasswordKeyValuePair()
    {
        const string input = "Server=db;Password=hunter2;Database=test";
        string redacted = SupportBundleSensitivePatternRedactor.RedactSensitivePatterns(input);

        redacted.Should().Contain("Password=[REDACTED]");
        redacted.Should().Contain("Server=db");
    }

    [Fact]
    public void RedactSensitivePatterns_NullOrEmpty_ReturnsEmpty()
    {
        SupportBundleSensitivePatternRedactor.RedactSensitivePatterns(null).Should().Be(string.Empty);
        SupportBundleSensitivePatternRedactor.RedactSensitivePatterns(string.Empty).Should().Be(string.Empty);
    }

    [Theory]
    [InlineData("ARCHLUCID_SQL_PASSWORD", true)]
    [InlineData("ARCHLUCID_API_KEY", true)]
    [InlineData("DOTNET_API_TOKEN", true)]
    [InlineData("ARCHLUCID_SECRET", true)]
    [InlineData("ARCHLUCID_API_URL", false)]
    [InlineData("DOTNET_ENVIRONMENT", false)]
    public void IsSensitiveEnvironmentVariableName_ClassifiesByPattern(string name, bool expected)
    {
        SupportBundleSensitivePatternRedactor.IsSensitiveEnvironmentVariableName(name).Should().Be(expected);
    }

    [Fact]
    public void RedactHttpUrl_StripsUserInfo()
    {
        SupportBundleSensitivePatternRedactor.RedactHttpUrl("https://user:pass@api.example.com/v1/foo")
            .Should().Be("https://api.example.com/v1/foo");
    }

    [Fact]
    public void RedactHttpUrl_NullOrEmpty_ReturnsEmpty()
    {
        SupportBundleSensitivePatternRedactor.RedactHttpUrl(null).Should().Be(string.Empty);
        SupportBundleSensitivePatternRedactor.RedactHttpUrl("").Should().Be(string.Empty);
    }
}
