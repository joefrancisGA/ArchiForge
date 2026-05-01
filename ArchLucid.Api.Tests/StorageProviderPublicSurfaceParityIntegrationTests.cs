using System.Net;

using ArchLucid.TestSupport;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Ensures <see cref="OpenApiContractWebAppFactory" /> (InMemory storage, no SQL) and
///     <see cref="GreenfieldSqlApiFactory" /> (Sql + DbUp) expose the same anonymous public HTTP surface
///     for health and OpenAPI â€” catches wiring regressions that break only one storage path.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
[Collection("ArchLucidEnvMutation")]
public sealed class StorageProviderPublicSurfaceParityIntegrationTests
{
    private const string SqlUnavailable =
        "SQL parity leg requires SQL Server. Set "
        + TestDatabaseEnvironment.ApiIntegrationSqlEnvironmentVariable
        + " or "
        + TestDatabaseEnvironment.PersistenceSqlEnvironmentVariable
        + " (see docs/BUILD.md).";

    private static bool IsSqlServerConfiguredForApiIntegration()
    {
        if (!string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable(TestDatabaseEnvironment.ApiIntegrationSqlEnvironmentVariable)))
            return true;

        return !string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable(TestDatabaseEnvironment.PersistenceSqlEnvironmentVariable)) || OperatingSystem.IsWindows();
    }

    [Fact]
    public async Task InMemory_host_exposes_live_ready_and_openapi()
    {
        await using OpenApiContractWebAppFactory factory = new();
        using HttpClient client = factory.CreateClient();

        (await client.GetAsync("/health/live")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/health/ready")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/openapi/v1.json")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Sql_greenfield_host_exposes_live_ready_and_openapi()
    {
        Assert.SkipUnless(IsSqlServerConfiguredForApiIntegration(), SqlUnavailable);

        await using GreenfieldSqlApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        (await client.GetAsync("/health/live")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/health/ready")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/openapi/v1.json")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
