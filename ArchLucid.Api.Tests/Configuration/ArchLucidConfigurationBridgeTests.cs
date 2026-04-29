using ArchLucid.Host.Core.Configuration;

using FluentAssertions;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ArchLucid.Api.Tests.Configuration;

public sealed class ArchLucidConfigurationBridgeTests
{
    [Fact]
    public void ResolveArchLucidOptions_reads_ArchLucid_section_and_flat_storage()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["ArchLucid:StorageProvider"] = "Sql" })
            .Build();

        ArchLucidOptions resolved = ArchLucidConfigurationBridge.ResolveArchLucidOptions(configuration);

        resolved.StorageProvider.Should().Be("Sql");
    }

    [Fact]
    public void ResolveArchLucidOptions_does_not_read_legacy_product_section()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["Archi" + "Forge:StorageProvider"] = "Sql" })
            .Build();

        ArchLucidOptions resolved = ArchLucidConfigurationBridge.ResolveArchLucidOptions(configuration);

        resolved.StorageProvider.Should().BeNullOrEmpty();
    }

    [Fact]
    public void ResolveAuthConfigurationValue_reads_ArchLucidAuth_only()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["ArchLucidAuth:Mode"] = "JwtBearer" })
            .Build();

        string? mode = ArchLucidConfigurationBridge.ResolveAuthConfigurationValue(configuration, "Mode");

        mode.Should().Be("JwtBearer");
    }

    [Fact]
    public void ResolveAuthConfigurationValue_does_not_read_legacy_auth_section()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["Archi" + "Forge" + "Auth:Mode"] = "JwtBearer" })
            .Build();

        string? mode = ArchLucidConfigurationBridge.ResolveAuthConfigurationValue(configuration, "Mode");

        mode.Should().BeNull();
    }

    [Fact]
    public void ResolveSqlConnectionString_reads_ArchLucid_only()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:ArchLucid"] = "Server=.;Database=x;" })
            .Build();

        string? cs = ArchLucidConfigurationBridge.ResolveSqlConnectionString(configuration);

        cs.Should().NotBeNull();
        SqlConnectionStringBuilder parsed = new(cs!);
        parsed.Encrypt.Should().Be(SqlConnectionEncryptOption.Mandatory);
        parsed.DataSource.Should().Be(".");
        parsed.InitialCatalog.Should().Be("x");
    }

    [Fact]
    public void ResolveSqlConnectionString_does_not_read_legacy_connection_string()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:" + "Archi" + "Forge"] = "Server=legacy;" })
            .Build();

        string? cs = ArchLucidConfigurationBridge.ResolveSqlConnectionString(configuration);

        cs.Should().BeNull();
    }
}
