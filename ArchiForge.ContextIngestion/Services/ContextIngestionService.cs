using ArchiForge.ContextIngestion.Canonicalization;
using ArchiForge.ContextIngestion.Infrastructure;
using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.ContextIngestion.Models;
using ArchiForge.ContextIngestion.Summaries;

namespace ArchiForge.ContextIngestion.Services;

/// <summary>
/// Orchestrates the context ingestion pipeline: collects raw objects from
/// <see cref="IContextConnector"/> instances in the order supplied by DI (the host uses
/// <see cref="ContextConnectorPipeline.ResolveOrdered"/>), canonicalizes and deduplicates them,
/// then persists the resulting <see cref="ContextSnapshot"/> for downstream pipeline stages.
/// </summary>
public class ContextIngestionService(
    IEnumerable<IContextConnector> connectors,
    ICanonicalEnricher enricher,
    ICanonicalDeduplicator deduplicator,
    IContextSnapshotRepository snapshotRepository,
    IContextDeltaSummaryBuilder deltaSummaryBuilder)
    : IContextIngestionService
{
    public async Task<ContextSnapshot> IngestAsync(
        ContextIngestionRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.RunId == Guid.Empty)
            throw new ArgumentException("RunId must be a non-empty GUID.", nameof(request));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ProjectId, nameof(request));

        ContextSnapshot snapshot = new()
        {
            SnapshotId = Guid.NewGuid(),
            RunId = request.RunId,
            ProjectId = request.ProjectId,
            CreatedUtc = DateTime.UtcNow
        };

        // Latest persisted snapshot for this project (any prior run), used for connector delta messaging.
        ContextSnapshot? previous = await snapshotRepository.GetLatestAsync(request.ProjectId, ct);

        List<CanonicalObject> allObjects = [];
        List<string> deltaSummaries = [];
        int connectorIndex = 0;

        foreach (IContextConnector connector in connectors)
        {
            RawContextPayload raw = await connector.FetchAsync(request, ct);
            NormalizedContextBatch normalized = await connector.NormalizeAsync(raw, ct);
            ContextDelta delta = await connector.DeltaAsync(normalized, previous, ct);

            allObjects.AddRange(normalized.CanonicalObjects);
            snapshot.Warnings.AddRange(normalized.Warnings);

            string segment = deltaSummaryBuilder.BuildSegment(
                connector.ConnectorType,
                delta.Summary,
                normalized,
                previous,
                isFirstConnector: connectorIndex == 0);
            deltaSummaries.Add(segment);
            connectorIndex++;
        }

        IReadOnlyList<CanonicalObject> enriched = enricher.Enrich(allObjects);
        snapshot.CanonicalObjects = deduplicator.Deduplicate(enriched).ToList();
        snapshot.DeltaSummary = string.Join("; ", deltaSummaries);

        return snapshot;
    }
}
