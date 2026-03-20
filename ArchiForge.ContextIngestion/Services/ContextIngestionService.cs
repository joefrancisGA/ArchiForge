using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.ContextIngestion.Models;

namespace ArchiForge.ContextIngestion.Services;

public class ContextIngestionService(
    IEnumerable<IContextConnector> connectors,
    IContextSnapshotRepository repo)
    : IContextIngestionService
{
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

        var previous = await repo.GetLatestAsync(request.ProjectId, ct);

        foreach (var connector in connectors)
        {
            var raw = await connector.FetchAsync(request, ct);
            var normalized = await connector.NormalizeAsync(raw, ct);
            var delta = await connector.DeltaAsync(normalized, previous, ct);

            snapshot.CanonicalObjects.AddRange(normalized.CanonicalObjects);
            snapshot.DeltaSummary = delta.Summary;
        }

        return snapshot;
    }
}

