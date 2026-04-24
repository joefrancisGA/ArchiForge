using ArchLucid.Host.Core.Startup.Validation;

using FluentAssertions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Moq;

namespace ArchLucid.Api.Tests;

[Collection("ArchLucidEnvMutation")]
public sealed class ArchLucidAllowRlsBypassConfigurationRulesTests
{
    [Fact]
    public void CollectErrors_WhenStagingAndSqlWithoutRlsButAllowBypass_skips_row_level_security_error()
    {
        string? prior = Environment.GetEnvironmentVariable("ARCHLUCID_ALLOW_RLS_BYPASS");

        try
        {
            Environment.SetEnvironmentVariable("ARCHLUCID_ALLOW_RLS_BYPASS", "true");

            Dictionary<string, string?> data = new()
            {
                ["ArchLucid:StorageProvider"] = "Sql",
                ["ArchLucid:Persistence:AllowRlsBypass"] = "true",
                ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
                ["ConnectionStrings:ArchLucid"] =
                    "Server=.;Database=x;Trusted_Connection=True;TrustServerCertificate=True",
                ["SqlServer:RowLevelSecurity:ApplySessionContext"] = "false",
                ["WebhookDelivery:UseHttpClient"] = "false"
            };

            IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
            Mock<IWebHostEnvironment> env = new();
            env.Setup(e => e.EnvironmentName).Returns(Environments.Staging);

            IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

            errors.Should()
                .NotContain(e => e.Contains("SqlServer:RowLevelSecurity:ApplySessionContext=true",
                    StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Environment.SetEnvironmentVariable("ARCHLUCID_ALLOW_RLS_BYPASS", prior);
        }
    }

    [Fact]
    public void CollectErrors_WhenProductionApiAndSqlWithoutRlsButAllowBypass_skips_row_level_security_error()
    {
        string? prior = Environment.GetEnvironmentVariable("ARCHLUCID_ALLOW_RLS_BYPASS");

        try
        {
            Environment.SetEnvironmentVariable("ARCHLUCID_ALLOW_RLS_BYPASS", "true");

            Dictionary<string, string?> data = new()
            {
                ["ArchLucid:StorageProvider"] = "Sql",
                ["ArchLucid:Persistence:AllowRlsBypass"] = "true",
                ["ArchLucidAuth:Mode"] = "JwtBearer",
                ["ArchLucidAuth:Authority"] = "https://login.example.com",
                ["ConnectionStrings:ArchLucid"] =
                    "Server=.;Database=x;Trusted_Connection=True;TrustServerCertificate=True",
                ["SqlServer:RowLevelSecurity:ApplySessionContext"] = "false",
                ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
                ["WebhookDelivery:UseHttpClient"] = "false"
            };

            IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
            Mock<IWebHostEnvironment> env = new();
            env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

            IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

            errors.Should()
                .NotContain(e => e.Contains("SqlServer:RowLevelSecurity:ApplySessionContext=true",
                    StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Environment.SetEnvironmentVariable("ARCHLUCID_ALLOW_RLS_BYPASS", prior);
        }
    }
}
