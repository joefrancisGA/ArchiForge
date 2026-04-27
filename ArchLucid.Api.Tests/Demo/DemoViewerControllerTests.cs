using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using ArchLucid.Api.Models;
using ArchLucid.Application.Bootstrap;
using ArchLucid.Core.Scoping;
using ArchLucid.TestSupport;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

namespace ArchLucid.Api.Tests.Demo;

/// <summary>
///     Anonymous demo viewer read surface (<c>/v1/demo/viewer/*</c>) when <c>Demo:AnonymousViewer:Enabled</c> is set.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
[Collection("ArchLucidEnvMutation")]
public sealed class DemoViewerControllerTests
{
    private const string SqlUnavailable =
        "API greenfield SQL tests need SQL Server. Set "
        + TestDatabaseEnvironment.ApiIntegrationSqlEnvironmentVariable
        + " or "
        + TestDatabaseEnvironment.PersistenceSqlEnvironmentVariable
        + " (see docs/BUILD.md), or use Windows with LocalDB.";

    private static bool IsSqlServerConfiguredForApiIntegration()
    {
        if (!string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable(TestDatabaseEnvironment.ApiIntegrationSqlEnvironmentVariable)))
            return true;

        return !string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable(TestDatabaseEnvironment.PersistenceSqlEnvironmentVariable))
            || OperatingSystem.IsWindows();
    }

    [SkippableFact]
    public async Task When_viewer_disabled_Get_runs_returns_401()
    {
        Skip.IfNot(IsSqlServerConfiguredForApiIntegration(), SqlUnavailable);

        await using GreenfieldSqlApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/v1/demo/viewer/runs");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [SkippableFact]
    public async Task When_viewer_enabled_and_seeded_Get_runs_returns_demo_shape()
    {
        Skip.IfNot(IsSqlServerConfiguredForApiIntegration(), SqlUnavailable);

        await using DemoViewerEnabledSqlApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage ready = await client.GetAsync("/health/ready");
        ready.StatusCode.Should().Be(HttpStatusCode.OK);

        using (IServiceScope scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IDemoSeedService>().SeedAsync();
        }

        HttpResponseMessage response = await client.GetAsync("/v1/demo/viewer/runs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement root = doc.RootElement;

        root.ValueKind.Should().Be(JsonValueKind.Array);
        root.GetArrayLength().Should().BeGreaterThan(0);

        JsonElement first = root[0];
        first.TryGetProperty("runId", out JsonElement runIdEl).Should().BeTrue();
        runIdEl.GetString().Should().NotBeNullOrWhiteSpace();
    }

    [SkippableFact]
    public async Task When_viewer_enabled_Post_returns_405()
    {
        Skip.IfNot(IsSqlServerConfiguredForApiIntegration(), SqlUnavailable);

        await using DemoViewerEnabledSqlApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        using (IServiceScope scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IDemoSeedService>().SeedAsync();
        }

        HttpResponseMessage response =
            await client.PostAsync("/v1/demo/viewer/runs", JsonContent.Create(new
            {
            }));

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [SkippableFact]
    public async Task When_viewer_enabled_Get_run_detail_returns_run()
    {
        Skip.IfNot(IsSqlServerConfiguredForApiIntegration(), SqlUnavailable);

        await using DemoViewerEnabledSqlApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        Guid tenantId;

        using (IServiceScope scope = factory.Services.CreateScope())
        {
            tenantId = scope.ServiceProvider.GetRequiredService<IScopeContextProvider>().GetCurrentScope().TenantId;
            await scope.ServiceProvider.GetRequiredService<IDemoSeedService>().SeedAsync();
        }

        ContosoRetailDemoIds demo = ContosoRetailDemoIds.ForTenant(tenantId);

        HttpResponseMessage response = await client.GetAsync($"/v1/demo/viewer/runs/{demo.RunBaseline}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        RunDetailsResponse? body = await response.Content.ReadFromJsonAsync<RunDetailsResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        body.Should().NotBeNull();
        body.Run.RunId.Should().NotBeNullOrWhiteSpace();
    }
}
