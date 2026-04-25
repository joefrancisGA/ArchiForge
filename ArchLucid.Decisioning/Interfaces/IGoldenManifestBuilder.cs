using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Decisioning.Models;
using ArchLucid.KnowledgeGraph.Models;

namespace ArchLucid.Decisioning.Interfaces;

public interface IGoldenManifestBuilder
{
    GoldenManifest Build(
        Guid runId,
        Guid contextSnapshotId,
        GraphSnapshot graphSnapshot,
        FindingsSnapshot findingsSnapshot,
        DecisionTrace trace,
        DecisionRuleSet ruleSet);
}
