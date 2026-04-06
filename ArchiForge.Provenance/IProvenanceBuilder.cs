using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.Decisioning.Models;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.Provenance;

public interface IProvenanceBuilder
{
    /// <summary>Builds a structural provenance graph for one authority run (captured during execution).</summary>
    DecisionProvenanceGraph Build(
        Guid runId,
        FindingsSnapshot findings,
        GraphSnapshot graph,
        GoldenManifest manifest,
        RuleAuditTrace trace,
        IReadOnlyList<SynthesizedArtifact> artifacts);
}
