using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.Configuration;

using FluentAssertions;

using Microsoft.Extensions.Configuration;

namespace ArchiForge.Api.Tests.Configuration;

public sealed class ArchiForgeAuthConfigurationBridgeTests
{
    [Fact]
    public void Resolve_merges_ArchLucidAuth_over_legacy()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ArchiForgeAuth:Mode"] = "ApiKey",
                    ["ArchiForgeAuth:Audience"] = "api://legacy",
                    ["ArchLucidAuth:Mode"] = "JwtBearer",
                    ["ArchLucidAuth:Authority"] = "https://login.example/"
                })
            .Build();

        ArchiForgeAuthOptions options = ArchiForgeAuthConfigurationBridge.Resolve(configuration);

        options.Mode.Should().Be("JwtBearer");
        options.Authority.Should().Be("https://login.example/");
        options.Audience.Should().Be("api://legacy");
    }
}
