using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.ContextIngestion.Models;

namespace ArchiForge.ContextIngestion.Connectors;

public class TopologyHintsConnector : IContextConnector
{
    public string ConnectorType => "topology-hints";

    public Task<RawContextPayload> FetchAsync(
        ContextIngestionRequest request,
        CancellationToken ct)
    {
        _ = ct;
        return Task.FromResult(new RawContextPayload
        {
            TopologyHints = request.TopologyHints.ToList()
        });
    }

    public Task<NormalizedContextBatch> NormalizeAsync(
        RawContextPayload payload,
        CancellationToken ct)
    {
        _ = ct;
        NormalizedContextBatch batch = new();

        foreach (string hint in payload.TopologyHints)
        {
            batch.CanonicalObjects.Add(new CanonicalObject
            {
                ObjectType = "TopologyResource",
                Name = hint,
                SourceType = "TopologyHint",
                SourceId = "topology-hint",
                Properties = new Dictionary<string, string>
                {
                    ["text"] = hint
                }
            });
        }

        return Task.FromResult(batch);
    }

    public Task<ContextDelta> DeltaAsync(
        NormalizedContextBatch current,
        ContextSnapshot? previous,
        CancellationToken ct)
    {
        _ = current;
        _ = ct;
        return Task.FromResult(new ContextDelta
        {
            Summary = previous is null ? "Initial topology hint ingestion" : "Updated topology hint ingestion"
        });
    }
}
