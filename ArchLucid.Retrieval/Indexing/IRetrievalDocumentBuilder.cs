using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.Core.Conversation;
using ArchiForge.Decisioning.Models;
using ArchiForge.Provenance;
using ArchiForge.Retrieval.Models;

namespace ArchiForge.Retrieval.Indexing;

/// <summary>
/// Maps domain objects into <see cref="ArchiForge.Retrieval.Models.RetrievalDocument"/> rows for <see cref="IRetrievalIndexingService"/>.
/// </summary>
/// <remarks>Implementation: <see cref="RetrievalDocumentBuilder"/> (JSON for manifest/provenance, plain text for artifacts/messages).</remarks>
public interface IRetrievalDocumentBuilder
{
    /// <summary>One document: full <see cref="GoldenManifest"/> JSON as content.</summary>
    /// <param name="manifest">Golden manifest to index (scope ids are taken from the manifest).</param>
    /// <returns>Single-element list containing the manifest document.</returns>
    IReadOnlyList<RetrievalDocument> BuildForManifest(GoldenManifest manifest);

    /// <summary>One document per synthesized artifact (content = artifact body).</summary>
    /// <param name="tenantId">Scope tenant.</param>
    /// <param name="workspaceId">Scope workspace.</param>
    /// <param name="projectId">Scope project.</param>
    /// <param name="artifacts">Artifacts to convert (empty input returns empty list).</param>
    /// <returns>One <see cref="RetrievalDocument"/> per artifact.</returns>
    IReadOnlyList<RetrievalDocument> BuildForArtifacts(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        IReadOnlyList<SynthesizedArtifact> artifacts);

    /// <summary>One document per chat message (title = role, content = message text).</summary>
    /// <param name="tenantId">Scope tenant.</param>
    /// <param name="workspaceId">Scope workspace.</param>
    /// <param name="projectId">Scope project.</param>
    /// <param name="runId">Optional run anchor for the conversation.</param>
    /// <param name="messages">Conversation messages to index.</param>
    /// <returns>One <see cref="RetrievalDocument"/> per message.</returns>
    IReadOnlyList<RetrievalDocument> BuildForConversation(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid? runId,
        IReadOnlyList<ConversationMessage> messages);

    /// <summary>Single document containing serialized <see cref="DecisionProvenanceGraph"/>.</summary>
    /// <param name="tenantId">Scope tenant.</param>
    /// <param name="workspaceId">Scope workspace.</param>
    /// <param name="projectId">Scope project.</param>
    /// <param name="runId">Run that produced the graph.</param>
    /// <param name="graph">Provenance graph to serialize and index.</param>
    /// <returns>Single-element list containing the provenance document.</returns>
    IReadOnlyList<RetrievalDocument> BuildForProvenance(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid runId,
        DecisionProvenanceGraph graph);
}
