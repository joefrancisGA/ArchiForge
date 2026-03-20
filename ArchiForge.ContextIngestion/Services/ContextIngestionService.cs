using ArchiForge.ContextIngestion.Canonicalization;
using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.ContextIngestion.Models;

namespace ArchiForge.ContextIngestion.Services;

public class ContextIngestionService : IContextIngestionService
{
    private readonly IEnumerable<IContextConnector> _connectors;
    private readonly ICanonicalDeduplicator _deduplicator;
    private readonly IContextSnapshotRepository _snapshotRepository;

    public ContextIngestionService(
        IEnumerable<IContextConnector> connectors,
        ICanonicalDeduplicator deduplicator,
        IContextSnapshotRepository snapshotRepository)
    {
        _connectors = connectors;
        _deduplicator = deduplicator;
        _snapshotRepository = snapshotRepository;
    }

    public async Task<ContextSnapshot> IngestAsync(
        ContextIngestionRequest request,
        CancellationToken ct)
    {
        var snapshot = new ContextSnapshot
        {
            SnapshotId = Guid.NewGuid(),
            RunId = request.RunId,
            ProjectId = request.ProjectId,
            CreatedUtc = DateTime.UtcNow
        };

        // Latest persisted snapshot for this project (any prior run), used for connector delta messaging.
        var previous = await _snapshotRepository.GetLatestAsync(request.ProjectId, ct);

        var allObjects = new List<CanonicalObject>();
        var deltaSummaries = new List<string>();

        foreach (var connector in _connectors)
        {
            var raw = await connector.FetchAsync(request, ct);
            var normalized = await connector.NormalizeAsync(raw, ct);
            var delta = await connector.DeltaAsync(normalized, previous, ct);

            allObjects.AddRange(normalized.CanonicalObjects);
            snapshot.Warnings.AddRange(normalized.Warnings);

            if (!string.IsNullOrWhiteSpace(delta.Summary))
            {
                deltaSummaries.Add(delta.Summary);
            }
        }

        snapshot.CanonicalObjects = _deduplicator.Deduplicate(allObjects).ToList();
        snapshot.DeltaSummary = string.Join("; ", deltaSummaries);

        return snapshot;
    }
}
