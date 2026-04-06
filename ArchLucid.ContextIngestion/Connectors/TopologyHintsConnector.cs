using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.ContextIngestion.Models;
using ArchiForge.ContextIngestion.Topology;

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
            string trimmed = hint.Trim();
            Dictionary<string, string> properties = new(StringComparer.OrdinalIgnoreCase)
            {
                ["text"] = trimmed
            };

            int slash = trimmed.IndexOf('/');
            if (slash > 0 && slash < trimmed.Length - 1)
            {
                string parentName = trimmed[..slash].Trim();
                string childRemainder = trimmed[(slash + 1)..].Trim();

                if (parentName.Length > 0 && childRemainder.Length > 0)
                {
                    // parentNodeId must match GraphNodeFactory: obj-{CanonicalObject.ObjectId}
                    string parentObjId = TopologyHintStableObjectIds.FromHintName(parentName);
                    properties["parentNodeId"] = $"obj-{parentObjId}";
                }
            }

            batch.CanonicalObjects.Add(new CanonicalObject
            {
                ObjectId = TopologyHintStableObjectIds.FromHintName(trimmed),
                ObjectType = "TopologyResource",
                Name = trimmed,
                SourceType = "TopologyHint",
                SourceId = "topology-hint",
                Properties = properties
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
