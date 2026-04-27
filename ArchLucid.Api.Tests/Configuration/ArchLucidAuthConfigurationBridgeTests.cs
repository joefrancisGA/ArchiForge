using ArchLucid.Api.Auth.Models;
using ArchLucid.Api.Configuration;

using FluentAssertions;

using Microsoft.Extensions.Configuration;

namespace ArchLucid.Api.Tests.Configuration;

public sealed class ArchLucidAuthConfigurationBridgeTests
{
    [Fact]
    public void Resolve_merges_ArchLucidAuth_over_legacy()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ArchLucidAuth:Mode"] = "ApiKey",
                    ["ArchLucidAuth:Audience"] = "api://legacy"
                })
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ArchLucidAuth:Mode"] = "JwtBearer",
                    ["ArchLucidAuth:Authority"] = "https://login.example/"
                })
            .Build();

        ArchLucidAuthOptions options = ArchLucidAuthConfigurationBridge.Resolve(configuration);

        options.Mode.Should().Be("JwtBearer");
        options.Authority.Should().Be("https://login.example/");
        options.Audience.Should().Be("api://legacy");
    }
}
