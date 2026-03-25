using FluentAssertions;

namespace ArchiForge.Cli.Tests;

public sealed class ArchiForgeApiClientTests
{
    [Fact]
    public void ResolveBaseUrl_WhenConfigHasApiUrl_ReturnsConfigUrl()
    {
        ArchiForgeProjectScaffolder.ArchiForgeConfig config = new ArchiForgeProjectScaffolder.ArchiForgeConfig
        {
            ApiUrl = "https://custom:9090"
        };

        string result = ArchiForgeApiClient.ResolveBaseUrl(config);

        result.Should().Be("https://custom:9090");
    }

    [Fact]
    public void ResolveBaseUrl_WhenConfigNull_ReturnsDefaultOrEnv()
    {
        string envKey = "ARCHIFORGE_API_URL";
        string? previous = Environment.GetEnvironmentVariable(envKey);
        try
        {
            Environment.SetEnvironmentVariable(envKey, null);
            string result = ArchiForgeApiClient.ResolveBaseUrl(null);
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
        ArchiForgeProjectScaffolder.ArchiForgeConfig config = new ArchiForgeProjectScaffolder.ArchiForgeConfig
        {
            ApiUrl = "http://localhost:5128/"
        };

        string result = ArchiForgeApiClient.ResolveBaseUrl(config);

        result.Should().Be("http://localhost:5128");
    }
}
