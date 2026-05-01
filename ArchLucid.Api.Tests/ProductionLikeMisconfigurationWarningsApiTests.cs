using System.IO;
using System.Text;

using ArchLucid.Core.Hosting;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Validates <see cref="ProductionLikeHostingMisconfigurationAdvisor" /> against effective WebApplicationFactory
///     configuration (staging-like signals via <c>ARCHLUCID_ENVIRONMENT</c> while the host stays Development-safe).
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ProductionLikeMisconfigurationWarningsApiTests
{
    [SkippableFact]
    public void Development_host_with_archlucid_staging_env_surfaces_jwt_authority_advisory_from_bound_configuration()
    {
        using WebApplicationFactory<Program> factory = new OpenApiContractWebAppFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?> { ["ARCHLUCID_ENVIRONMENT"] = "Staging" });

                config.AddJsonStream(
                    new MemoryStream(
                        Encoding.UTF8.GetBytes(
                            """
                            {
                              "ArchLucid": {
                                "ContentSafety": {
                                  "Endpoint": "https://example.invalid/content-safety",
                                  "ApiKey": "01234567890123456789012345678901234567890123456789012345678901234"
                                }
                              },
                              "ArchLucidAuth": {
                                "Mode": "JwtBearer",
                                "Authority": "",
                                "JwtSigningPublicKeyPemPath": ""
                              },
                              "Authentication": {
                                "ApiKey": {
                                  "Enabled": false
                                }
                              }
                            }
                            """)));
            });
        });

        factory.CreateClient();

        IConfiguration configuration = factory.Services.GetRequiredService<IConfiguration>();
        IHostEnvironment environment = factory.Services.GetRequiredService<IHostEnvironment>();

        environment.EnvironmentName.Should().Be(Environments.Development);

        IReadOnlyList<string> warnings =
            ProductionLikeHostingMisconfigurationAdvisor.DescribeWarnings(configuration, environment);

        warnings.Should().Contain(w => w.Contains("ArchLucidAuth:Authority", StringComparison.OrdinalIgnoreCase));
    }
}
