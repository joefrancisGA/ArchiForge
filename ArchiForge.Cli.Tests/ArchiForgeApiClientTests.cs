using ArchiForge;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Cli.Tests;

public sealed class ArchiForgeApiClientTests
{
    [Fact]
    public void ResolveBaseUrl_WhenConfigHasApiUrl_ReturnsConfigUrl()
    {
        var config = new ArchiForgeProjectScaffolder.ArchiForgeConfig
        {
            ApiUrl = "https://custom:9090"
        };

        var result = ArchiForgeApiClient.ResolveBaseUrl(config);

        result.Should().Be("https://custom:9090");
    }

    [Fact]
    public void ResolveBaseUrl_WhenConfigNull_ReturnsDefaultOrEnv()
    {
        var envKey = "ARCHIFORGE_API_URL";
        var previous = Environment.GetEnvironmentVariable(envKey);
        try
        {
            Environment.SetEnvironmentVariable(envKey, null);
            var result = ArchiForgeApiClient.ResolveBaseUrl(null);
            result.Should().Be("http://localhost:5128");
        }
        finally
        {
            Environment.SetEnvironmentVariable(envKey, previous);
        }
    }

    [Fact]
    public void ResolveBaseUrl_WhenConfigHasApiUrlWithTrailingSlash_TrimsSlash()
    {
        var config = new ArchiForgeProjectScaffolder.ArchiForgeConfig
        {
            ApiUrl = "http://localhost:5128/"
        };

        var result = ArchiForgeApiClient.ResolveBaseUrl(config);

        result.Should().Be("http://localhost:5128");
    }
}
