using ArchiForge.Core.Conversation;

namespace ArchiForge.Host.Core.Ask;

/// <summary>
/// Application orchestration for Ask conversation threads and message persistence.
/// </summary>
/// <remarks>
/// Implementation: <see cref="ConversationService"/>. Used by <c>ArchiForge.Host.Core.Services.Ask.AskService</c>. Repositories: <c>ArchiForge.Persistence.Conversation.*</c>.
/// </remarks>
public interface IConversationService
{
    /// <summary>
    /// Loads an existing thread when <paramref name="threadId"/> is set and matches <paramref name="tenantId"/>/<paramref name="workspaceId"/>/<paramref name="projectId"/>; otherwise creates a new row with optional run anchors.
    /// </summary>
    /// <param name="threadId">Existing thread id, or <see langword="null"/> to create.</param>
    /// <param name="tenantId">Scope tenant.</param>
    /// <param name="workspaceId">Scope workspace.</param>
    /// <param name="projectId">Scope project.</param>
    /// <param name="runId">Primary run anchor for new threads.</param>
    /// <param name="baseRunId">Optional comparison base.</param>
    /// <param name="targetRunId">Optional comparison target.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The loaded or newly created thread.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="threadId"/> refers to a thread in a different scope.</exception>
    Task<ConversationThread> GetOrCreateThreadAsync(
        Guid? threadId,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid? runId,
        Guid? baseRunId,
        Guid? targetRunId,
        CancellationToken ct);

    /// <summary>
    /// Returns up to <paramref name="take"/> recent messages in chronological order (oldest first within the window).
    /// </summary>
    Task<IReadOnlyList<ConversationMessage>> GetHistoryAsync(Guid threadId, int take, CancellationToken ct);

    /// <summary>Appends a user turn and bumps <see cref="ConversationThread.LastUpdatedUtc"/>.</summary>
    Task AppendUserMessageAsync(Guid threadId, string content, CancellationToken ct);

    /// <summary>Appends an assistant turn with JSON metadata and bumps <see cref="ConversationThread.LastUpdatedUtc"/>.</summary>
    Task AppendAssistantMessageAsync(Guid threadId, string content, string metadataJson, CancellationToken ct);
}
