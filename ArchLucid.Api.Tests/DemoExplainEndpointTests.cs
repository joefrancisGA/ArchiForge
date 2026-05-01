using System.Net;
using System.Net.Http.Json;

using ArchLucid.Application.Bootstrap;
using ArchLucid.Core.Explanation;
using ArchLucid.Host.Core.Demo;
using ArchLucid.Provenance;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     HTTP coverage for <c>GET /v1/demo/explain</c> â€” the public proof endpoint that powers the operator-shell
///     <c>/demo/explain</c> route. Two scenarios exercise the security-critical 404 paths:
///     <list type="bullet">
///         <item>
///             <description>
///                 <b>Demo:Enabled=false</b> (factory default) â€” <see cref="FeatureGateFilter" /> short-circuits
///                 before the action runs.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Demo:Enabled=true</b> but no committed demo run â€” <see cref="DemoReadModelClient" />
///                 returns null and the controller emits <c>NotFoundProblem</c>.
///             </description>
///         </item>
///     </list>
///     A third scenario stubs <see cref="IDemoReadModelClient" /> to return a payload and confirms the endpoint serializes
///     it intact.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class DemoExplainEndpointTests : IClassFixture<ArchLucidApiFactory>
{
    private readonly ArchLucidApiFactory _factory;

    public DemoExplainEndpointTests(ArchLucidApiFactory factory)
    {
        _factory = factory;
    }

    [SkippableFact]
    public async Task GetDemoExplain_returns_404_when_demo_not_enabled_on_deployment()
    {
        // Factory defaults leave Demo:Enabled unset (false). The FeatureGateFilter must hide the route entirely.
        HttpClient client = _factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        HttpResponseMessage response = await client.GetAsync("/v1/demo/explain");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [SkippableFact]
    public async Task GetDemoExplain_returns_404_when_demo_enabled_but_no_committed_demo_run()
    {
        WebApplicationFactory<Program> enabled = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Demo:Enabled"] = "true" }));
        });

        HttpClient client = enabled.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        HttpResponseMessage response = await client.GetAsync("/v1/demo/explain");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [SkippableFact]
    public async Task GetDemoExplain_returns_200_with_payload_when_demo_enabled_and_read_model_resolves()
    {
        StubDemoReadModelClient stub = new();

        WebApplicationFactory<Program> enabled = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Demo:Enabled"] = "true" }));

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDemoReadModelClient>();
                services.AddScoped<IDemoReadModelClient>(_ => stub);
            });
        });

        HttpClient client = enabled.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        HttpResponseMessage response = await client.GetAsync("/v1/demo/explain");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        DemoExplainResponse? payload = await response.Content.ReadFromJsonAsync<DemoExplainResponse>();
        payload.Should().NotBeNull();
        payload.RunId.Should().Be(ContosoRetailDemoIdentifiers.AuthorityRunBaselineId.ToString("N"));
        payload.IsDemoData.Should().BeTrue();
        payload.RunExplanation.Should().NotBeNull();
        payload.ProvenanceGraph.Should().NotBeNull();
    }

    /// <summary>
    ///     Stub implementation that returns a deterministic payload. Lives in this test file because it is only used here
    ///     and stays well inside the suite's "tests are the only callers" rule.
    /// </summary>
    private sealed class StubDemoReadModelClient : IDemoReadModelClient
    {
        public Task<DemoExplainResponse?> GetLatestCommittedDemoExplainAsync(
            CancellationToken cancellationToken = default)
        {
            DemoExplainResponse response = new()
            {
                GeneratedUtc = DateTimeOffset.UtcNow,
                RunId = ContosoRetailDemoIdentifiers.AuthorityRunBaselineId.ToString("N"),
                ManifestVersion = ContosoRetailDemoIdentifiers.ManifestBaseline,
                IsDemoData = true,
                DemoStatusMessage = "demo tenant â€” replace before publishing",
                RunExplanation = new RunExplanationSummary
                {
                    Explanation = new ExplanationResult { Summary = "Stub" },
                    ThemeSummaries = ["Theme"],
                    OverallAssessment = "Healthy",
                    RiskPosture = "Moderate"
                },
                ProvenanceGraph = new GraphViewModel()
            };

            return Task.FromResult<DemoExplainResponse?>(response);
        }
    }
}
