using ArchiForge.Core.Conversation;

namespace ArchiForge.Persistence.Conversation;

/// <summary>
/// Thread-safe in-memory <see cref="IConversationMessageRepository"/> for tests and storage-off mode.
/// </summary>
public sealed class InMemoryConversationMessageRepository : IConversationMessageRepository
{
    private const int MaxEntries = 2000;
    private readonly List<ConversationMessage> _messages = [];

    /// <inheritdoc />
    public Task AddAsync(ConversationMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);
        ct.ThrowIfCancellationRequested();
        lock (_messages)
        {
            _messages.Add(message);
            if (_messages.Count > MaxEntries)
                _messages.RemoveRange(0, _messages.Count - MaxEntries);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ConversationMessage>> GetByThreadIdAsync(
        Guid threadId,
        int take,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        take = Math.Clamp(take, 1, 500);
        lock (_messages)
        {
            List<ConversationMessage> result = _messages
                .Where(x => x.ThreadId == threadId)
                .OrderByDescending(x => x.CreatedUtc)
                .Take(take)
                .OrderBy(x => x.CreatedUtc)
                .ToList();
            return Task.FromResult<IReadOnlyList<ConversationMessage>>(result);
        }
    }
}
