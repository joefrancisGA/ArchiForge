using ArchLucid.ContextIngestion.Canonicalization;
using ArchLucid.ContextIngestion.Interfaces;
using ArchLucid.ContextIngestion.Models;
using ArchLucid.ContextIngestion.Repositories;
using ArchLucid.ContextIngestion.Services;
using ArchLucid.ContextIngestion.Summaries;

using FluentAssertions;

namespace ArchLucid.ContextIngestion.Tests;

/// <summary>
///     Tests for Context Ingestion Service.
/// </summary>
[Trait("Suite", "Core")]
public sealed class ContextIngestionServiceTests
{
    [Fact]
    public async Task IngestAsync_ProducesEnrichedDeltaSummary()
    {
        InMemoryContextSnapshotRepository repo = new();
        IContextConnector[] connectors = [new CountingConnector()];
        ContextIngestionService sut = new(
            connectors,
            new CanonicalInfrastructureEnricher(),
            new CanonicalDeduplicator(),
            repo,
            new DefaultContextDeltaSummaryBuilder());

        ContextIngestionRequest request = new() { RunId = Guid.NewGuid(), ProjectId = "proj-ingest-test" };

        ContextSnapshot snapshot = await sut.IngestAsync(request, CancellationToken.None);

        snapshot.CanonicalObjects.Should().HaveCount(1);
        snapshot.DeltaSummary.Should().Contain("connector summary");
        snapshot.DeltaSummary.Should().Contain("test-connector");
        snapshot.DeltaSummary.Should().Contain("1 produced");
        snapshot.DeltaSummary.Should().Contain("Requirement×1");
    }

    private sealed class CountingConnector : IContextConnector
    {
        public string ConnectorType => "test-connector";

        public Task<RawContextPayload> FetchAsync(ContextIngestionRequest request, CancellationToken ct)
        {
            _ = request;
            _ = ct;
            return Task.FromResult(new RawContextPayload());
        }

        public Task<NormalizedContextBatch> NormalizeAsync(RawContextPayload payload, CancellationToken ct)
        {
            _ = payload;
            _ = ct;
            NormalizedContextBatch batch = new();
            batch.CanonicalObjects.Add(new CanonicalObject
            {
                ObjectType = "Requirement",
                Name = "from-test",
                SourceType = "Test",
                SourceId = "t",
                Properties = new Dictionary<string, string> { ["text"] = "hello" }
            });
            return Task.FromResult(batch);
        }

        public Task<ContextDelta> DeltaAsync(
            NormalizedContextBatch current,
            ContextSnapshot? previous,
            CancellationToken ct)
        {
            _ = current;
            _ = previous;
            _ = ct;
            return Task.FromResult(new ContextDelta { Summary = "connector summary" });
        }
    }
}
