using ArchLucid.Core.Hosting;

using FluentAssertions;

using Microsoft.Extensions.Configuration;

namespace ArchLucid.Core.Tests.Hosting;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ProductionLikeHostingMisconfigurationAdvisorTests
{
    [Fact]
    public void DescribeWarnings_pure_development_and_safe_archlucid_env_returns_empty()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(Array.Empty<KeyValuePair<string, string?>>())
            .Build();

        IReadOnlyList<string> warnings = ProductionLikeHostingMisconfigurationAdvisor.DescribeWarnings(
            configuration,
            Microsoft.Extensions.Hosting.Environments.Development);

        warnings.Should().BeEmpty();
    }

    [Fact]
    public void DescribeWarnings_development_with_archlucid_staging_warns_on_empty_cors_for_api_role()
    {
        Dictionary<string, string?> data = new()
        {
            ["ARCHLUCID_ENVIRONMENT"] = "Staging",
            ["Hosting:Role"] = "Api",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

        IReadOnlyList<string> warnings = ProductionLikeHostingMisconfigurationAdvisor.DescribeWarnings(
            configuration,
            Microsoft.Extensions.Hosting.Environments.Development);

        warnings.Should().Contain(w => w.Contains("Cors:AllowedOrigins", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DescribeWarnings_staging_jwt_without_authority_or_pem_warns()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucidAuth:Mode"] = "JwtBearer",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

        IReadOnlyList<string> warnings = ProductionLikeHostingMisconfigurationAdvisor.DescribeWarnings(
            configuration,
            Microsoft.Extensions.Hosting.Environments.Staging);

        warnings.Should().Contain(w => w.Contains("ArchLucidAuth:Authority", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DescribeWarnings_staging_api_key_disabled_warns()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucidAuth:Mode"] = "ApiKey",
            ["Authentication:ApiKey:Enabled"] = "false",
            ["Cors:AllowedOrigins:0"] = "https://ui.example",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

        IReadOnlyList<string> warnings = ProductionLikeHostingMisconfigurationAdvisor.DescribeWarnings(
            configuration,
            Microsoft.Extensions.Hosting.Environments.Staging);

        warnings.Should().Contain(w => w.Contains("Authentication:ApiKey:Enabled", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DescribeWarnings_worker_role_skips_cors_even_when_empty()
    {
        Dictionary<string, string?> data = new()
        {
            ["Hosting:Role"] = "Worker",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

        IReadOnlyList<string> warnings = ProductionLikeHostingMisconfigurationAdvisor.DescribeWarnings(
            configuration,
            Microsoft.Extensions.Hosting.Environments.Staging);

        warnings.Should().NotContain(w => w.Contains("Cors:AllowedOrigins", StringComparison.OrdinalIgnoreCase));
    }
}
