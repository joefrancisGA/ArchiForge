using System.Net;
using System.Security.Cryptography;

using ArchLucid.Application.Bootstrap;
using ArchLucid.Core.Explanation;
using ArchLucid.Host.Core.Demo;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ArchLucid.Api.Tests;

[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class DemoCommitPagePreviewEndpointTests : IClassFixture<ArchLucidApiFactory>
{
    private readonly ArchLucidApiFactory _factory;

    public DemoCommitPagePreviewEndpointTests(ArchLucidApiFactory factory) => _factory = factory;

    [Fact]
    public async Task GetDemoPreview_returns_404_when_demo_not_enabled()
    {
        HttpClient client = _factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        HttpResponseMessage response = await client.GetAsync("/v1/demo/preview");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDemoPreview_returns_404_when_demo_enabled_but_preview_unavailable()
    {
        WebApplicationFactory<Program> enabled = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Demo:Enabled"] = "true" }));
        });

        HttpClient client = enabled.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        HttpResponseMessage response = await client.GetAsync("/v1/demo/preview");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDemoPreview_returns_200_with_cache_headers_and_stable_body_on_cache_hit()
    {
        StubPreviewClient stub = new();

        WebApplicationFactory<Program> enabled = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Demo:Enabled"] = "true" }));

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDemoCommitPagePreviewClient>();
                services.AddScoped<IDemoCommitPagePreviewClient>(_ => stub);
            });
        });

        HttpClient client = enabled.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        HttpResponseMessage first = await client.GetAsync("/v1/demo/preview");
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        first.Headers.CacheControl!.ToString().Should().Be("public, max-age=300, s-maxage=300, stale-while-revalidate=60");
        first.Headers.ETag.Should().NotBeNull();

        byte[] firstBytes = await first.Content.ReadAsByteArrayAsync();

        HttpResponseMessage alias = await client.GetAsync("/v1/public/demo/sample-run");
        alias.StatusCode.Should().Be(HttpStatusCode.OK);
        (await alias.Content.ReadAsByteArrayAsync()).Should().Equal(firstBytes);

        HttpResponseMessage second = await client.GetAsync("/v1/demo/preview");
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        byte[] secondBytes = await second.Content.ReadAsByteArrayAsync();

        secondBytes.Should().Equal(firstBytes);

        string? etag = first.Headers.ETag?.Tag;
        etag.Should().NotBeNullOrWhiteSpace();

        using HttpRequestMessage cond = new(HttpMethod.Get, "/v1/demo/preview");
        cond.Headers.TryAddWithoutValidation("If-None-Match", etag);

        HttpResponseMessage third = await client.SendAsync(cond);
        third.StatusCode.Should().Be(HttpStatusCode.NotModified);
        (await third.Content.ReadAsByteArrayAsync()).Length.Should().Be(0);
    }

    [Fact]
    public async Task GetDemoPreview_etag_matches_sha256_of_json_body()
    {
        StubPreviewClient stub = new();

        WebApplicationFactory<Program> enabled = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Demo:Enabled"] = "true" }));

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDemoCommitPagePreviewClient>();
                services.AddScoped<IDemoCommitPagePreviewClient>(_ => stub);
            });
        });

        HttpClient client = enabled.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        HttpResponseMessage response = await client.GetAsync("/v1/demo/preview");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        byte[] body = await response.Content.ReadAsByteArrayAsync();
        string hex = Convert.ToHexString(SHA256.HashData(body)).ToLowerInvariant();
        string expectedQuoted = $"\"{hex}\"";

        response.Headers.ETag!.Tag.Should().Be(expectedQuoted);
    }

    private sealed class StubPreviewClient : IDemoCommitPagePreviewClient
    {
        private static readonly DateTimeOffset FixedGeneratedUtc = DateTimeOffset.Parse("2026-04-01T12:00:00Z");

        private static readonly DateTime FixedRowUtc = DateTime.SpecifyKind(new DateTime(2026, 3, 15, 8, 0, 0), DateTimeKind.Utc);

        public Task<DemoCommitPagePreviewResponse?> GetLatestCommittedDemoCommitPageAsync(CancellationToken cancellationToken = default)
        {
            Guid manifestId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            Guid runId = ContosoRetailDemoIdentifiers.AuthorityRunBaselineId;

            DemoCommitPagePreviewResponse response = new()
            {
                GeneratedUtc = FixedGeneratedUtc,
                IsDemoData = true,
                DemoStatusMessage = "demo tenant — replace before publishing",
                Run = new DemoPreviewRun
                {
                    RunId = runId.ToString("N"),
                    ProjectId = "default",
                    Description = "stub",
                    CreatedUtc = FixedRowUtc,
                },
                AuthorityChain = new DemoPreviewAuthorityChain
                {
                    ContextSnapshotId = null,
                    GraphSnapshotId = null,
                    FindingsSnapshotId = null,
                    GoldenManifestId = manifestId.ToString("N"),
                    DecisionTraceId = null,
                    ArtifactBundleId = null,
                },
                Manifest = new DemoPreviewManifestSummary
                {
                    ManifestId = manifestId.ToString("N"),
                    RunId = runId.ToString("N"),
                    CreatedUtc = FixedRowUtc,
                    ManifestHash = "mh",
                    RuleSetId = "r",
                    RuleSetVersion = "v",
                    DecisionCount = 1,
                    WarningCount = 0,
                    UnresolvedIssueCount = 0,
                    Status = "ok",
                    HasWarnings = false,
                    HasUnresolvedIssues = false,
                    OperatorSummary = "1 decisions, 0 warnings, 0 unresolved issues, status ok",
                },
                Artifacts = [],
                PipelineTimeline = [],
                RunExplanation = new RunExplanationSummary
                {
                    Explanation = new ExplanationResult { Summary = "stub" },
                    ThemeSummaries = ["t1"],
                    OverallAssessment = "a",
                    RiskPosture = "Low",
                },
            };

            return Task.FromResult<DemoCommitPagePreviewResponse?>(response);
        }
    }
}
