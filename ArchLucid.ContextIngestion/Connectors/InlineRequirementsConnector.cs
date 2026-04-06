using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.ContextIngestion.Models;

namespace ArchiForge.ContextIngestion.Connectors;

public class InlineRequirementsConnector : IContextConnector
{
    public string ConnectorType => "inline-requirements";

    public Task<RawContextPayload> FetchAsync(
        ContextIngestionRequest request,
        CancellationToken ct)
    {
        _ = ct;
        return Task.FromResult(new RawContextPayload
        {
            InlineRequirements = request.InlineRequirements.ToList()
        });
    }

    public Task<NormalizedContextBatch> NormalizeAsync(
        RawContextPayload payload,
        CancellationToken ct)
    {
        _ = ct;
        NormalizedContextBatch batch = new();

        foreach (string requirement in payload.InlineRequirements)
        
            batch.CanonicalObjects.Add(new CanonicalObject
            {
                ObjectType = "Requirement",
                Name = requirement.Length > 80 ? requirement[..80] : requirement,
                SourceType = "InlineRequirement",
                SourceId = "inline",
                Properties = new Dictionary<string, string>
                {
                    ["text"] = requirement
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
            Summary = previous is null ? "Initial inline requirement ingestion" : "Updated inline requirement ingestion"
        });
    }
}
