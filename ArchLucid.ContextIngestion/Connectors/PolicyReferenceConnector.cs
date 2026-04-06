using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.ContextIngestion.Models;
using ArchiForge.ContextIngestion.Topology;

namespace ArchiForge.ContextIngestion.Connectors;

public class PolicyReferenceConnector : IContextConnector
{
    /// <summary>Must match <c>CanonicalGraphPropertyKeys.ApplicableTopologyNodeIds</c> in the knowledge-graph project.</summary>
    private const string ApplicableTopologyNodeIdsKey = "applicableTopologyNodeIds";

    public string ConnectorType => "policy-reference";

    public Task<RawContextPayload> FetchAsync(
        ContextIngestionRequest request,
        CancellationToken ct)
    {
        _ = ct;
        return Task.FromResult(new RawContextPayload
        {
            PolicyReferences = request.PolicyReferences.ToList(),
            TopologyHints = request.TopologyHints.ToList()
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
            Dictionary<string, string> properties = new(StringComparer.OrdinalIgnoreCase)
            {
                ["reference"] = policy,
                ["status"] = "referenced"
            };

            string? targeted = BuildApplicableTopologyNodeIds(policy, payload.TopologyHints);
            if (!string.IsNullOrWhiteSpace(targeted))
                properties[ApplicableTopologyNodeIdsKey] = targeted;

            batch.CanonicalObjects.Add(new CanonicalObject
            {
                ObjectType = "PolicyControl",
                Name = policy,
                SourceType = "PolicyReference",
                SourceId = policy,
                Properties = properties
            });
        }

        return Task.FromResult(batch);
    }

    /// <summary>
    /// When a topology hint name overlaps the policy reference (substring, case-insensitive),
    /// links the policy to <c>obj-{stableId}</c> for that hint so graph inference can narrow <c>APPLIES_TO</c>.
    /// </summary>
    private static string? BuildApplicableTopologyNodeIds(string policyReference, List<string> topologyHints)
    {
        if (topologyHints.Count == 0)
            return null;

        HashSet<string> ids = [];

        foreach (string? trimmed in from hint in topologyHints where !string.IsNullOrWhiteSpace(hint) select hint.Trim() into trimmed where PolicyReferenceOverlapsTopology(policyReference, trimmed) select trimmed)
        
            ids.Add($"obj-{TopologyHintStableObjectIds.FromHintName(trimmed)}");
        

        return ids.Count == 0 ? null : string.Join(',', ids);
    }

    private static bool PolicyReferenceOverlapsTopology(string policyReference, string topologyHint)
    {
        return topologyHint.Contains(policyReference, StringComparison.OrdinalIgnoreCase)
            || policyReference.Contains(topologyHint, StringComparison.OrdinalIgnoreCase);
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
