using ArchiForge.ContextIngestion.Canonicalization;
using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.ContextIngestion.Models;
using ArchiForge.ContextIngestion.Repositories;
using ArchiForge.ContextIngestion.Services;
using ArchiForge.ContextIngestion.Summaries;

using FluentAssertions;

namespace ArchiForge.ContextIngestion.Tests;

public sealed class ContextIngestionServiceTests
{
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
            NormalizedContextBatch batch = new NormalizedContextBatch();
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

    [Fact]
    public async Task IngestAsync_ProducesEnrichedDeltaSummary()
    {
        InMemoryContextSnapshotRepository repo = new InMemoryContextSnapshotRepository();
        IContextConnector[] connectors = [new CountingConnector()];
        ContextIngestionService sut = new ContextIngestionService(
            connectors,
            new CanonicalInfrastructureEnricher(),
            new CanonicalDeduplicator(),
            repo,
            new DefaultContextDeltaSummaryBuilder());

        ContextIngestionRequest request = new ContextIngestionRequest
        {
            RunId = Guid.NewGuid(),
            ProjectId = "proj-ingest-test"
        };

        ContextSnapshot snapshot = await sut.IngestAsync(request, CancellationToken.None);

        snapshot.CanonicalObjects.Should().HaveCount(1);
        snapshot.DeltaSummary.Should().Contain("connector summary");
        snapshot.DeltaSummary.Should().Contain("test-connector");
        snapshot.DeltaSummary.Should().Contain("1 produced");
        snapshot.DeltaSummary.Should().Contain("Requirement×1");
    }
}
