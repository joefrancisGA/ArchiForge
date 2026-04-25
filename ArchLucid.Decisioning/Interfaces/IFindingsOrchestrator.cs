using ArchLucid.Decisioning.Models;
using ArchLucid.KnowledgeGraph.Models;

namespace ArchLucid.Decisioning.Interfaces;

public interface IFindingsOrchestrator
{
    Task<FindingsSnapshot> GenerateFindingsSnapshotAsync(
        Guid runId,
        Guid contextSnapshotId,
        GraphSnapshot graphSnapshot,
        CancellationToken ct);
}
