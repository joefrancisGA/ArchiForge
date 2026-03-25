using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.Decisioning.Models;
using ArchiForge.Provenance;
using ArchiForge.Retrieval.Models;

namespace ArchiForge.Retrieval.Indexing;

/// <summary>
/// <see cref="IRetrievalRunCompletionIndexer"/> orchestration: <see cref="IRetrievalDocumentBuilder"/> (manifest, artifacts, provenance) then <see cref="IRetrievalIndexingService"/>.
/// </summary>
public sealed class RetrievalRunCompletionIndexer(
    IRetrievalDocumentBuilder documentBuilder,
    IRetrievalIndexingService indexingService) : IRetrievalRunCompletionIndexer
{
    /// <inheritdoc />
    public async Task IndexAuthorityRunAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        GoldenManifest manifest,
        IReadOnlyList<SynthesizedArtifact> artifacts,
        DecisionProvenanceGraph provenanceGraph,
        CancellationToken ct)
    {
        List<RetrievalDocument> retrievalDocuments = new List<RetrievalDocument>();
        retrievalDocuments.AddRange(documentBuilder.BuildForManifest(manifest));
        retrievalDocuments.AddRange(documentBuilder.BuildForArtifacts(
            tenantId,
            workspaceId,
            projectId,
            artifacts));
        retrievalDocuments.AddRange(documentBuilder.BuildForProvenance(
            tenantId,
            workspaceId,
            projectId,
            manifest.RunId,
            provenanceGraph));

        await indexingService.IndexDocumentsAsync(retrievalDocuments, ct).ConfigureAwait(false);
    }
}
