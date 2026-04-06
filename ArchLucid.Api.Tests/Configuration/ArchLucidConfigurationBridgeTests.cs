using ArchiForge.Host.Core.Configuration;

using FluentAssertions;

using Microsoft.Extensions.Configuration;

namespace ArchiForge.Api.Tests.Configuration;

public sealed class ArchiForgeConfigurationBridgeTests
{
    [Fact]
    public void ResolveArchiForgeOptions_ArchLucid_storage_overrides_legacy()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ArchiForge:StorageProvider"] = "InMemory",
                    ["ArchLucid:StorageProvider"] = "Sql"
                })
            .Build();

        ArchiForgeOptions resolved = ArchiForgeConfigurationBridge.ResolveArchiForgeOptions(configuration);

        resolved.StorageProvider.Should().Be("Sql");
    }

    [Fact]
    public void ResolveAuthConfigurationValue_prefers_ArchLucidAuth()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ArchiForgeAuth:Mode"] = "ApiKey",
                    ["ArchLucidAuth:Mode"] = "JwtBearer"
                })
            .Build();

        string? mode = ArchiForgeConfigurationBridge.ResolveAuthConfigurationValue(configuration, "Mode");

        mode.Should().Be("JwtBearer");
    }

    [Fact]
    public void ResolveAuthConfigurationValue_falls_back_when_lucid_empty()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ArchiForgeAuth:Mode"] = "ApiKey"
                })
            .Build();

        string? mode = ArchiForgeConfigurationBridge.ResolveAuthConfigurationValue(configuration, "Mode");

        mode.Should().Be("ApiKey");
    }
}
