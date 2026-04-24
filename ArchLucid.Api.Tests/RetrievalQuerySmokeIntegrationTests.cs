using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using ArchLucid.Core.Scoping;
using ArchLucid.Retrieval.Indexing;
using ArchLucid.Retrieval.Models;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

namespace ArchLucid.Api.Tests;

/// <summary>
///     End-to-end: index documents via DI → query via <c>GET v1/retrieval/search</c> → assert hits.
///     Uses <see cref="AlertLifecycleWebAppFactory" /> (InMemory storage + <c>FakeEmbeddingService</c> +
///     <c>InMemoryVectorIndex</c>).
/// </summary>
[Trait("Category", "Integration")]
public sealed class RetrievalQuerySmokeIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task Index_documents_then_query_returns_matching_hits()
    {
        await using AlertLifecycleWebAppFactory factory = new();

        await SeedRetrievalDocumentsAsync(factory.Services);

        HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync(
            new Uri("v1/retrieval/search?q=microservices+topology&topK=5", UriKind.Relative),
            CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<RetrievalHit>? hits = await response.Content
            .ReadFromJsonAsync<List<RetrievalHit>>(JsonOptions, CancellationToken.None);

        hits.Should().NotBeNull();
        hits.Should().NotBeEmpty("indexed documents should produce at least one retrieval hit");
    }

    [Fact]
    public async Task Query_without_q_returns_bad_request()
    {
        await using AlertLifecycleWebAppFactory factory = new();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            new Uri("v1/retrieval/search?q=", UriKind.Relative),
            CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Query_with_no_indexed_documents_returns_empty_list()
    {
        await using AlertLifecycleWebAppFactory factory = new();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            new Uri("v1/retrieval/search?q=anything&topK=3", UriKind.Relative),
            CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<RetrievalHit>? hits = await response.Content
            .ReadFromJsonAsync<List<RetrievalHit>>(JsonOptions, CancellationToken.None);

        hits.Should().NotBeNull();
        hits.Should().BeEmpty("no documents have been indexed");
    }

    [Fact]
    public async Task TopK_clamps_result_count()
    {
        await using AlertLifecycleWebAppFactory factory = new();
        await SeedRetrievalDocumentsAsync(factory.Services);

        HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync(
            new Uri("v1/retrieval/search?q=architecture&topK=1", UriKind.Relative),
            CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<RetrievalHit>? hits = await response.Content
            .ReadFromJsonAsync<List<RetrievalHit>>(JsonOptions, CancellationToken.None);

        hits.Should().NotBeNull();
        hits.Should().HaveCountLessThanOrEqualTo(1);
    }

    private static async Task SeedRetrievalDocumentsAsync(IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        IRetrievalIndexingService indexingService =
            scope.ServiceProvider.GetRequiredService<IRetrievalIndexingService>();

        List<RetrievalDocument> documents =
        [
            new()
            {
                DocumentId = "doc-arch-001",
                TenantId = ScopeIds.DefaultTenant,
                WorkspaceId = ScopeIds.DefaultWorkspace,
                ProjectId = ScopeIds.DefaultProject,
                RunId = null,
                ManifestId = null,
                SourceType = "Manifest",
                SourceId = "manifest-001",
                Title = "Architecture Topology",
                Content =
                    "The system uses a microservices topology with three primary services: API Gateway, Order Service, and Payment Service.",
                ContentHash = "hash-001",
                CreatedUtc = DateTime.UtcNow
            },
            new()
            {
                DocumentId = "doc-arch-002",
                TenantId = ScopeIds.DefaultTenant,
                WorkspaceId = ScopeIds.DefaultWorkspace,
                ProjectId = ScopeIds.DefaultProject,
                RunId = null,
                ManifestId = null,
                SourceType = "Artifact",
                SourceId = "artifact-001",
                Title = "Security Baseline",
                Content =
                    "All inter-service communication is encrypted using mTLS. No public SMB (port 445) exposure is permitted.",
                ContentHash = "hash-002",
                CreatedUtc = DateTime.UtcNow
            }
        ];

        await indexingService.IndexDocumentsAsync(documents, CancellationToken.None);
    }
}
