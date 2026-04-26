using System.Net;
using System.Net.Http.Json;

using ArchLucid.Application.Bootstrap;
using ArchLucid.Contracts.Findings;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Queries;

using FluentAssertions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArchLucid.Api.Tests;

/// <summary>
///     HTTP coverage for <c>GET /v1/findings/{findingId}/inspect</c> (ReadAuthority, in-memory or SQL read-model).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class FindingInspectEndpointTests : IntegrationTestBase
{
    public FindingInspectEndpointTests(ArchLucidApiFactory factory)
        : base(factory)
    {
    }

    private static string DemoPrimaryFindingId =>
        $"finding-demo-{ContosoRetailDemoIdentifiers.AuthorityRunBaselineId:N}-primary";

    /// <summary>
    ///     Startup seed is optional (Demo:SeedOnStartup / Demo:Enabled may be off via user secrets); tests that need
    ///     Contoso baseline rows must seed explicitly like <see cref="DemoSeedServiceTests" />.
    /// </summary>
    private async Task EnsureDemoBaselineSeededAsync()
    {
        using IServiceScope serviceScope = Factory.Services.CreateScope();
        await serviceScope.ServiceProvider.GetRequiredService<IDemoSeedService>().SeedAsync();
    }

    /// <summary>Regression: after explicit seed, authority read-model exposes findings for the inspect repository.</summary>
    [Fact]
    public async Task Demo_baseline_run_detail_has_findings_after_explicit_seed()
    {
        await EnsureDemoBaselineSeededAsync();

        using IServiceScope serviceScope = Factory.Services.CreateScope();
        IAuthorityQueryService authority = serviceScope.ServiceProvider.GetRequiredService<IAuthorityQueryService>();
        IScopeContextProvider scopeProvider = serviceScope.ServiceProvider.GetRequiredService<IScopeContextProvider>();
        ScopeContext scope = scopeProvider.GetCurrentScope();

        RunDetailDto? detail = await authority.GetRunDetailAsync(
            scope,
            ContosoRetailDemoIdentifiers.AuthorityRunBaselineId,
            CancellationToken.None);

        detail.Should().NotBeNull();
        detail.FindingsSnapshot.Should().NotBeNull();
        detail.FindingsSnapshot!.Findings.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetFindingInspect_when_seeded_returns_200_with_contract_shape()
    {
        await EnsureDemoBaselineSeededAsync();

        HttpResponseMessage response = await Client.GetAsync($"/v1/findings/{DemoPrimaryFindingId}/inspect");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        FindingInspectResponse? body = await response.Content.ReadFromJsonAsync<FindingInspectResponse>(JsonOptions);
        body.Should().NotBeNull();
        body.FindingId.Should().Be(DemoPrimaryFindingId);
        body.RunId.Should().Be(ContosoRetailDemoIdentifiers.AuthorityRunBaselineId);
        body.ManifestVersion.Should().NotBeNullOrWhiteSpace();
        body.DecisionRuleId.Should().Be("demo-seed-rule");
        body.Evidence.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFindingInspect_when_unknown_returns_404()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/findings/does-not-exist/inspect");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetFindingInspect_anonymous_api_key_mode_returns_401()
    {
        await using HealthEndpointSecurityApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        WireDefaultSqlIntegrationScopeHeaders(client);

        HttpResponseMessage response = await client.GetAsync($"/v1/findings/{DemoPrimaryFindingId}/inspect");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetFindingInspect_when_role_outside_read_authority_returns_403()
    {
        await using NoReadAuthorityRoleApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        WireDefaultSqlIntegrationScopeHeaders(client);

        HttpResponseMessage response = await client.GetAsync($"/v1/findings/{DemoPrimaryFindingId}/inspect");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    ///     DevelopmentBypass principal with a role that does not satisfy
    ///     <see cref="ArchLucid.Core.Authorization.ArchLucidPolicies.ReadAuthority" />.
    /// </summary>
    private sealed class NoReadAuthorityRoleApiFactory : ArchLucidApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ArchLucidAuth:DevRole"] = "GuestNoRead" }));
        }
    }
}
