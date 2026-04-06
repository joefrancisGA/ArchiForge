using ArchiForge.Decisioning.Models;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.Decisioning.Interfaces;

public interface IGoldenManifestBuilder
{
    GoldenManifest Build(
        Guid runId,
        Guid contextSnapshotId,
        GraphSnapshot graphSnapshot,
        FindingsSnapshot findingsSnapshot,
        RuleAuditTrace trace,
        DecisionRuleSet ruleSet);
}

