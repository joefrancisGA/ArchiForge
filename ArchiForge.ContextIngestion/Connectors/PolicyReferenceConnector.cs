using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.ContextIngestion.Models;

namespace ArchiForge.ContextIngestion.Connectors;

public class PolicyReferenceConnector : IContextConnector
{
    public string ConnectorType => "policy-reference";

    public Task<RawContextPayload> FetchAsync(
        ContextIngestionRequest request,
        CancellationToken ct)
    {
        _ = ct;
        return Task.FromResult(new RawContextPayload
        {
            PolicyReferences = request.PolicyReferences.ToList()
        });
    }

    public Task<NormalizedContextBatch> NormalizeAsync(
        RawContextPayload payload,
        CancellationToken ct)
    {
        _ = ct;
        NormalizedContextBatch batch = new();

        foreach (string policy in payload.PolicyReferences)
        {
            batch.CanonicalObjects.Add(new CanonicalObject
            {
                ObjectType = "PolicyControl",
                Name = policy,
                SourceType = "PolicyReference",
                SourceId = policy,
                Properties = new Dictionary<string, string>
                {
                    ["reference"] = policy,
                    ["status"] = "referenced"
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
            Summary = previous is null ? "Initial policy ingestion" : "Updated policy ingestion"
        });
    }
}
