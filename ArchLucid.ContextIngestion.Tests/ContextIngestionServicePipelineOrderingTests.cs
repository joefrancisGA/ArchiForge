using ArchLucid.ContextIngestion.Canonicalization;
using ArchLucid.ContextIngestion.Interfaces;
using ArchLucid.ContextIngestion.Models;
using ArchLucid.ContextIngestion.Repositories;
using ArchLucid.ContextIngestion.Services;
using ArchLucid.ContextIngestion.Summaries;

using FluentAssertions;

namespace ArchLucid.ContextIngestion.Tests;

/// <summary>
///     Regression tests for connector iteration order in <see cref="ContextIngestionService" />:
///     <see cref="ContextSnapshot.DeltaSummary" /> segment order and warning propagation.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class ContextIngestionServicePipelineOrderingTests
{
    [Fact]
    public async Task IngestAsync_DeltaSummarySegments_FollowConnectorOrder()
    {
        string markerA = $"SEG_{Guid.NewGuid():N}_A";
        string markerB = $"SEG_{Guid.NewGuid():N}_B";
        string markerC = $"SEG_{Guid.NewGuid():N}_C";

        IContextConnector[] connectors =
        [
            new MarkerConnector("c-first", markerA),
            new MarkerConnector("c-second", markerB, "WARN_FROM_SECOND_CONNECTOR"),
            new MarkerConnector("c-third", markerC)
        ];

        ContextIngestionService sut = new(
            connectors,
            new CanonicalInfrastructureEnricher(),
            new CanonicalDeduplicator(),
            new InMemoryContextSnapshotRepository(),
            new DefaultContextDeltaSummaryBuilder());

        ContextIngestionRequest request = new() { RunId = Guid.NewGuid(), ProjectId = "proj-order-test" };

        ContextSnapshot snapshot = await sut.IngestAsync(request, CancellationToken.None);

        snapshot.DeltaSummary.Should().NotBeNull();
        string summary = snapshot.DeltaSummary!;

        int indexA = summary.IndexOf(markerA, StringComparison.Ordinal);
        int indexB = summary.IndexOf(markerB, StringComparison.Ordinal);
        int indexC = summary.IndexOf(markerC, StringComparison.Ordinal);

        indexA.Should().BeGreaterThanOrEqualTo(0);
        indexB.Should().BeGreaterThanOrEqualTo(0);
        indexC.Should().BeGreaterThanOrEqualTo(0);
        indexA.Should().BeLessThan(indexB);
        indexB.Should().BeLessThan(indexC);

        snapshot.Warnings.Should().ContainSingle().Which.Should().Be("WARN_FROM_SECOND_CONNECTOR");
    }

    [Fact]
    public async Task IngestAsync_Warnings_FollowConnectorOrder()
    {
        IContextConnector[] connectors =
        [
            new MarkerConnector("w1", "S1", "WARN_CONNECTOR_1"),
            new MarkerConnector("w2", "S2", "WARN_CONNECTOR_2"),
            new MarkerConnector("w3", "S3", "WARN_CONNECTOR_3")
        ];

        ContextIngestionService sut = new(
            connectors,
            new CanonicalInfrastructureEnricher(),
            new CanonicalDeduplicator(),
            new InMemoryContextSnapshotRepository(),
            new DefaultContextDeltaSummaryBuilder());

        ContextIngestionRequest request = new() { RunId = Guid.NewGuid(), ProjectId = "proj-warnings-order" };

        ContextSnapshot snapshot = await sut.IngestAsync(request, CancellationToken.None);

        snapshot.Warnings.Should().Equal("WARN_CONNECTOR_1", "WARN_CONNECTOR_2", "WARN_CONNECTOR_3");
    }

    private sealed class MarkerConnector(string connectorType, string markerSummary, string? warning = null)
        : IContextConnector
    {
        public string ConnectorType
        {
            get;
        } = connectorType;

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

            if (warning is not null)
                batch.Warnings.Add(warning);

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
            return Task.FromResult(new ContextDelta { Summary = markerSummary });
        }
    }
}
