using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.ContextIngestion.Models;

namespace ArchiForge.ContextIngestion.Connectors;

public class SecurityBaselineHintsConnector : IContextConnector
{
    public string ConnectorType => "security-baseline-hints";

    public Task<RawContextPayload> FetchAsync(
        ContextIngestionRequest request,
        CancellationToken ct)
    {
        _ = ct;
        return Task.FromResult(new RawContextPayload
        {
            SecurityBaselineHints = request.SecurityBaselineHints.ToList()
        });
    }

    public Task<NormalizedContextBatch> NormalizeAsync(
        RawContextPayload payload,
        CancellationToken ct)
    {
        _ = ct;
        NormalizedContextBatch batch = new();

        foreach (string hint in payload.SecurityBaselineHints)
        
            batch.CanonicalObjects.Add(new CanonicalObject
            {
                ObjectType = "SecurityBaseline",
                Name = hint,
                SourceType = "SecurityBaselineHint",
                SourceId = "security-hint",
                Properties = new Dictionary<string, string>
                {
                    ["text"] = hint,
                    ["status"] = "declared"
                }
            });
        

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
            Summary = previous is null ? "Initial security baseline hint ingestion" : "Updated security baseline hint ingestion"
        });
    }
}
