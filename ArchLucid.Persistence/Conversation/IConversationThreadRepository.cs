using ArchLucid.Core.Conversation;

namespace ArchLucid.Persistence.Conversation;

/// <summary>
///     Persistence for <see cref="ConversationThread" /> rows used by Ask.
/// </summary>
/// <remarks>
///     SQL: <see cref="DapperConversationThreadRepository" />; in-memory:
///     <see cref="InMemoryConversationThreadRepository" />.
///     Primary caller: <c>ArchLucid.Api.Ask.ConversationService</c>.
/// </remarks>
public interface IConversationThreadRepository
{
    /// <summary>Inserts a new thread row.</summary>
    Task CreateAsync(ConversationThread thread, CancellationToken ct);

    /// <summary>Loads by id, or <see langword="null" /> if missing.</summary>
    Task<ConversationThread?> GetByIdAsync(Guid threadId, CancellationToken ct);

    /// <summary>Lists recent threads for a scope, newest <see cref="ConversationThread.LastUpdatedUtc" /> first.</summary>
    Task<IReadOnlyList<ConversationThread>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        int take,
        CancellationToken ct);

    /// <summary>Paged listing with total count for HTTP pagination without loading the full thread list.</summary>
    Task<(IReadOnlyList<ConversationThread> Items, int TotalCount)> ListByScopePagedAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        int skip,
        int take,
        CancellationToken ct);

    /// <summary>
    ///     Sets <see cref="ConversationThread.ArchivedUtc" /> for threads with <c>LastUpdatedUtc</c> strictly before
    ///     <paramref name="cutoffUtc" /> that are not yet archived. Returns rows updated.
    /// </summary>
    Task<int> ArchiveThreadsLastUpdatedBeforeAsync(DateTimeOffset cutoffUtc, CancellationToken ct);

    /// <summary>Sets <see cref="ConversationThread.LastUpdatedUtc" /> after a message append.</summary>
    Task UpdateLastUpdatedAsync(Guid threadId, DateTime updatedUtc, CancellationToken ct);
}
