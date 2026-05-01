using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Core.Explanation;
using ArchLucid.Host.Core.Demo;
using ArchLucid.Host.Core.Marketing;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ArchLucid.Api.Tests;

[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class MarketingShowcaseEndpointTests : IClassFixture<ArchLucidApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter(null) }
    };

    private readonly ArchLucidApiFactory _factory;

    public MarketingShowcaseEndpointTests(ArchLucidApiFactory factory)
    {
        _factory = factory;
    }

    [SkippableFact]
    public async Task GetShowcase_returns_404_when_stub_returns_null()
    {
        WebApplicationFactory<Program> app = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPublicShowcaseCommitPageClient>();
                services.AddScoped<IPublicShowcaseCommitPageClient>(_ => new NullShowcaseClient());
            });
        });

        HttpClient client = app.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        HttpResponseMessage response = await client.GetAsync("/v1/marketing/showcase/contoso-baseline");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [SkippableFact]
    public async Task GetShowcase_returns_200_when_stub_returns_payload()
    {
        WebApplicationFactory<Program> app = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPublicShowcaseCommitPageClient>();
                services.AddScoped<IPublicShowcaseCommitPageClient>(_ => new StubShowcaseClient());
            });
        });

        HttpClient client = app.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        HttpResponseMessage response = await client.GetAsync("/v1/marketing/showcase/contoso-baseline");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        DemoCommitPagePreviewResponse? body =
            await response.Content.ReadFromJsonAsync<DemoCommitPagePreviewResponse>(JsonOptions);

        body.Should().NotBeNull();
        body.Run.RunId.Should().NotBeNullOrWhiteSpace();
    }

    private sealed class NullShowcaseClient : IPublicShowcaseCommitPageClient
    {
        public Task<DemoCommitPagePreviewResponse?> GetShowcaseCommitPageAsync(Guid runId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DemoCommitPagePreviewResponse?>(null);
        }
    }

    private sealed class StubShowcaseClient : IPublicShowcaseCommitPageClient
    {
        public Task<DemoCommitPagePreviewResponse?> GetShowcaseCommitPageAsync(Guid runId,
            CancellationToken cancellationToken = default)
        {
            DateTimeOffset generatedUtc = DateTimeOffset.Parse("2026-04-01T12:00:00Z", CultureInfo.InvariantCulture);
            DateTime rowUtc = DateTime.SpecifyKind(new DateTime(2026, 3, 15, 8, 0, 0), DateTimeKind.Utc);
            Guid manifestId = Guid.Parse("11111111-1111-1111-1111-111111111111", CultureInfo.InvariantCulture);

            DemoCommitPagePreviewResponse response = new()
            {
                GeneratedUtc = generatedUtc,
                IsDemoData = true,
                DemoStatusMessage = "stub",
                Run =
                    new DemoPreviewRun
                    {
                        RunId = runId.ToString("N"),
                        ProjectId = "default",
                        Description = "stub",
                        CreatedUtc = rowUtc
                    },
                AuthorityChain = new DemoPreviewAuthorityChain { GoldenManifestId = manifestId.ToString("N") },
                Manifest = new DemoPreviewManifestSummary
                {
                    ManifestId = manifestId.ToString("N"),
                    RunId = runId.ToString("N"),
                    CreatedUtc = rowUtc,
                    ManifestHash = "h",
                    RuleSetId = "r",
                    RuleSetVersion = "v",
                    DecisionCount = 0,
                    WarningCount = 0,
                    UnresolvedIssueCount = 0,
                    Status = "ok",
                    HasWarnings = false,
                    HasUnresolvedIssues = false,
                    OperatorSummary = "stub"
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
