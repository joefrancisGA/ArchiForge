using ArchiForge.Core.Conversation;

namespace ArchiForge.Persistence.Conversation;

/// <summary>
/// Thread-safe in-memory <see cref="IConversationThreadRepository"/> for tests and storage-off mode.
/// </summary>
public sealed class InMemoryConversationThreadRepository : IConversationThreadRepository
{
    private const int MaxEntries = 500;
    private readonly List<ConversationThread> _threads = [];

    /// <inheritdoc />
    public Task CreateAsync(ConversationThread thread, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(thread);
        ct.ThrowIfCancellationRequested();
        lock (_threads)
        {
            _threads.Add(thread);
            if (_threads.Count > MaxEntries)
                _threads.RemoveRange(0, _threads.Count - MaxEntries);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<ConversationThread?> GetByIdAsync(Guid threadId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (_threads)
            return Task.FromResult(_threads.FirstOrDefault(x => x.ThreadId == threadId));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ConversationThread>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        int take,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        take = Math.Clamp(take, 1, 200);
        lock (_threads)
        {
            List<ConversationThread> result = _threads
                .Where(x => x.TenantId == tenantId &&
                            x.WorkspaceId == workspaceId &&
                            x.ProjectId == projectId)
                .OrderByDescending(x => x.LastUpdatedUtc)
                .Take(take)
                .ToList();
            return Task.FromResult<IReadOnlyList<ConversationThread>>(result);
        }
    }

    /// <inheritdoc />
    public Task UpdateLastUpdatedAsync(Guid threadId, DateTime updatedUtc, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (_threads)
        {
            ConversationThread? thread = _threads.FirstOrDefault(x => x.ThreadId == threadId);
            if (thread is not null)
                thread.LastUpdatedUtc = updatedUtc;
        }

        return Task.CompletedTask;
    }
}
