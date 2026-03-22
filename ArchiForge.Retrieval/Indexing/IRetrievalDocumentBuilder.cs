using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.Core.Conversation;
using ArchiForge.Decisioning.Models;
using ArchiForge.Provenance;
using ArchiForge.Retrieval.Models;

namespace ArchiForge.Retrieval.Indexing;

public interface IRetrievalDocumentBuilder
{
    IReadOnlyList<RetrievalDocument> BuildForManifest(GoldenManifest manifest);

    IReadOnlyList<RetrievalDocument> BuildForArtifacts(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        IReadOnlyList<SynthesizedArtifact> artifacts);

    IReadOnlyList<RetrievalDocument> BuildForConversation(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid? runId,
        IReadOnlyList<ConversationMessage> messages);

    IReadOnlyList<RetrievalDocument> BuildForProvenance(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid runId,
        DecisionProvenanceGraph graph);
}
