using ArchLucid.ContextIngestion.Models;
using ArchLucid.KnowledgeGraph.Models;

namespace ArchLucid.KnowledgeGraph.Interfaces;

public interface IKnowledgeGraphService
{
    Task<GraphSnapshot> BuildSnapshotAsync(
        ContextSnapshot contextSnapshot,
        CancellationToken ct);
}
