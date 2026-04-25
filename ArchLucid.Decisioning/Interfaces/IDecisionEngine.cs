using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Decisioning.Models;
using ArchLucid.KnowledgeGraph.Models;

namespace ArchLucid.Decisioning.Interfaces;

public interface IDecisionEngine
{
    Task<(GoldenManifest Manifest, DecisionTrace Trace)> DecideAsync(
        Guid runId,
        Guid contextSnapshotId,
        GraphSnapshot graphSnapshot,
        FindingsSnapshot findingsSnapshot,
        CancellationToken ct);
}
