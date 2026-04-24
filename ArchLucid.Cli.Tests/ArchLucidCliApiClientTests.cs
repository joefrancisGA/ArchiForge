using FluentAssertions;

namespace ArchLucid.Cli.Tests;

/// <summary>
///     Tests for <see cref="ArchLucidApiClient" /> URL resolution.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ArchLucidApiClientTests
{
    [Fact]
    public void ResolveBaseUrl_WhenConfigHasApiUrl_ReturnsConfigUrl()
    {
        ArchLucidProjectScaffolder.ArchLucidCliConfig config = new() { ApiUrl = "https://custom:9090" };

        string result = ArchLucidApiClient.ResolveBaseUrl(config);

        result.Should().Be("https://custom:9090");
    }

    [Fact]
    public void ResolveBaseUrl_WhenConfigNull_ReturnsDefaultWhenEnvUnset()
    {
        string? priorLucid = Environment.GetEnvironmentVariable("ARCHLUCID_API_URL");

        try
        {
            Environment.SetEnvironmentVariable("ARCHLUCID_API_URL", null);
            string result = ArchLucidApiClient.ResolveBaseUrl(null);
            result.Should().Be("http://localhost:5128");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ARCHLUCID_API_URL", priorLucid);
        }
    }

    [Fact]
    public void ResolveBaseUrl_WhenConfigNull_uses_ARCHLUCID_API_URL()
    {
        string? priorLucid = Environment.GetEnvironmentVariable("ARCHLUCID_API_URL");

        try
        {
            Environment.SetEnvironmentVariable("ARCHLUCID_API_URL", "http://from-env:7070");

            string result = ArchLucidApiClient.ResolveBaseUrl(null);

            result.Should().Be("http://from-env:7070");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ARCHLUCID_API_URL", priorLucid);
        }
    }

    [Fact]
    public void ResolveBaseUrl_WhenConfigHasApiUrlWithTrailingSlash_TrimsSlash()
    {
        ArchLucidProjectScaffolder.ArchLucidCliConfig config = new() { ApiUrl = "http://localhost:5128/" };

        string result = ArchLucidApiClient.ResolveBaseUrl(config);

        result.Should().Be("http://localhost:5128");
    }
}
