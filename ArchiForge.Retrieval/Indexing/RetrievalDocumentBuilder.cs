using System.Text.Json;

using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.Core.Conversation;
using ArchiForge.Decisioning.Models;
using ArchiForge.Provenance;
using ArchiForge.Retrieval.Models;

namespace ArchiForge.Retrieval.Indexing;

/// <summary>
/// <see cref="IRetrievalDocumentBuilder"/> with stable <see cref="RetrievalDocument.DocumentId"/> patterns per source type.
/// </summary>
public sealed class RetrievalDocumentBuilder : IRetrievalDocumentBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc />
    public IReadOnlyList<RetrievalDocument> BuildForManifest(GoldenManifest manifest)
    {
        return
        [
            new RetrievalDocument
            {
                DocumentId = $"manifest-{manifest.ManifestId:N}",
                TenantId = manifest.TenantId,
                WorkspaceId = manifest.WorkspaceId,
                ProjectId = manifest.ProjectId,
                RunId = manifest.RunId,
                ManifestId = manifest.ManifestId,
                SourceType = "Manifest",
                SourceId = manifest.ManifestId.ToString(),
                Title = manifest.Metadata.Name,
                Content = JsonSerializer.Serialize(manifest, JsonOptions),
                ContentHash = manifest.ManifestHash,
                CreatedUtc = manifest.CreatedUtc
            }
        ];
    }

    /// <inheritdoc />
    public IReadOnlyList<RetrievalDocument> BuildForArtifacts(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        IReadOnlyList<SynthesizedArtifact> artifacts) =>
        artifacts.Select(x => new RetrievalDocument
        {
            DocumentId = $"artifact-{x.ArtifactId:N}",
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            RunId = x.RunId,
            ManifestId = x.ManifestId,
            SourceType = "Artifact",
            SourceId = x.ArtifactId.ToString(),
            Title = x.Name,
            Content = x.Content,
            ContentHash = x.ContentHash,
            CreatedUtc = x.CreatedUtc
        }).ToList();

    /// <inheritdoc />
    public IReadOnlyList<RetrievalDocument> BuildForConversation(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid? runId,
        IReadOnlyList<ConversationMessage> messages) =>
        messages.Select(x => new RetrievalDocument
        {
            DocumentId = $"conversation-{x.MessageId:N}",
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            RunId = runId,
            SourceType = "ConversationMessage",
            SourceId = x.MessageId.ToString(),
            Title = x.Role,
            Content = x.Content,
            ContentHash = x.MessageId.ToString("N"),
            CreatedUtc = x.CreatedUtc
        }).ToList();

    /// <inheritdoc />
    public IReadOnlyList<RetrievalDocument> BuildForProvenance(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid runId,
        DecisionProvenanceGraph graph)
    {
        string summary = JsonSerializer.Serialize(graph, JsonOptions);

        return
        [
            new RetrievalDocument
            {
                DocumentId = $"provenance-{runId:N}",
                TenantId = tenantId,
                WorkspaceId = workspaceId,
                ProjectId = projectId,
                RunId = runId,
                SourceType = "ProvenanceGraph",
                SourceId = runId.ToString(),
                Title = $"Provenance for Run {runId}",
                Content = summary,
                ContentHash = runId.ToString("N"),
                CreatedUtc = DateTime.UtcNow
            }
        ];
    }
}
