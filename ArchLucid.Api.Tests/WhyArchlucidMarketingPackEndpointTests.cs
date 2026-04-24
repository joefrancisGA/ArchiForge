using System.Net;

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

/// <summary>
///     HTTP coverage for <c>GET /v1/marketing/why-archlucid-pack.pdf</c> — anonymous, demo-gated PDF bundle for the public
///     <c>/why</c> page.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class WhyArchlucidMarketingPackEndpointTests : IClassFixture<ArchLucidApiFactory>
{
    private readonly ArchLucidApiFactory _factory;

    public WhyArchlucidMarketingPackEndpointTests(ArchLucidApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetWhyArchlucidPackPdf_returns_404_when_demo_not_enabled()
    {
        HttpClient client = _factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        HttpResponseMessage response = await client.GetAsync("/v1/marketing/why-archlucid-pack.pdf");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetWhyArchlucidPackPdf_returns_404_when_demo_enabled_but_preview_unavailable()
    {
        WebApplicationFactory<Program> enabled = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Demo:Enabled"] = "true" }));

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDemoCommitPagePreviewClient>();
                services.AddScoped<IDemoCommitPagePreviewClient>(_ => new NullPreviewClient());
            });
        });

        HttpClient client = enabled.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        HttpResponseMessage response = await client.GetAsync("/v1/marketing/why-archlucid-pack.pdf");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetWhyArchlucidPackPdf_returns_pdf_when_demo_enabled_and_preview_resolves()
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

        HttpResponseMessage response = await client.GetAsync("/v1/marketing/why-archlucid-pack.pdf");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");

        byte[] body = await response.Content.ReadAsByteArrayAsync();
        body.Length.Should().BeGreaterThan(500);
        ReadOnlySpan<byte> head = body.AsSpan(0, Math.Min(5, body.Length));
        head.SequenceEqual("%PDF-"u8).Should().BeTrue();
    }

    private sealed class NullPreviewClient : IDemoCommitPagePreviewClient
    {
        public Task<DemoCommitPagePreviewResponse?> GetLatestCommittedDemoCommitPageAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DemoCommitPagePreviewResponse?>(null);
        }
    }

    private sealed class StubPreviewClient : IDemoCommitPagePreviewClient
    {
        private static readonly DateTimeOffset FixedGeneratedUtc = DateTimeOffset.Parse("2026-04-01T12:00:00Z");

        private static readonly DateTime FixedRowUtc =
            DateTime.SpecifyKind(new DateTime(2026, 3, 15, 8, 0, 0), DateTimeKind.Utc);

        public Task<DemoCommitPagePreviewResponse?> GetLatestCommittedDemoCommitPageAsync(
            CancellationToken cancellationToken = default)
        {
            Guid manifestId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            Guid runId = ContosoRetailDemoIdentifiers.AuthorityRunBaselineId;

            DemoCommitPagePreviewResponse response = new()
            {
                GeneratedUtc = FixedGeneratedUtc,
                IsDemoData = true,
                DemoStatusMessage = "demo tenant — replace before publishing",
                Run =
                    new DemoPreviewRun
                    {
                        RunId = runId.ToString("N"),
                        ProjectId = "default",
                        Description = "stub",
                        CreatedUtc = FixedRowUtc
                    },
                AuthorityChain =
                    new DemoPreviewAuthorityChain
                    {
                        ContextSnapshotId = null,
                        GraphSnapshotId = null,
                        FindingsSnapshotId = null,
                        GoldenManifestId = manifestId.ToString("N"),
                        DecisionTraceId = null,
                        ArtifactBundleId = null
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
                    OperatorSummary = "1 decisions, 0 warnings, 0 unresolved issues, status ok"
                },
                Artifacts = [],
                PipelineTimeline = [],
                RunExplanation = new RunExplanationSummary
                {
                    Explanation = new ExplanationResult { Summary = "stub" },
                    ThemeSummaries = ["t1"],
                    OverallAssessment = "a",
                    RiskPosture = "Low"
                }
            };

            return Task.FromResult<DemoCommitPagePreviewResponse?>(response);
        }
    }
}
