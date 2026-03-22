using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.Decisioning.Models;
using ArchiForge.Provenance;

namespace ArchiForge.Retrieval.Indexing;

/// <summary>Indexes manifest, artifacts, and provenance after an authority run completes.</summary>
public interface IRetrievalRunCompletionIndexer
{
    Task IndexAuthorityRunAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        GoldenManifest manifest,
        IReadOnlyList<SynthesizedArtifact> artifacts,
        DecisionProvenanceGraph provenanceGraph,
        CancellationToken ct);
}
