using ArchLucid.Decisioning.Models;
using ArchLucid.KnowledgeGraph.Models;

namespace ArchLucid.Decisioning.Interfaces;

public interface IFindingEngine
{
    string EngineType
    {
        get;
    }

    string Category
    {
        get;
    }

    Task<IReadOnlyList<Finding>> AnalyzeAsync(
        GraphSnapshot graphSnapshot,
        CancellationToken ct);
}
