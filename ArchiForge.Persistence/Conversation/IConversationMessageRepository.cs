using ArchiForge.Core.Conversation;

namespace ArchiForge.Persistence.Conversation;

public interface IConversationMessageRepository
{
    Task AddAsync(ConversationMessage message, CancellationToken ct);

    /// <summary>Most recent <paramref name="take"/> messages in chronological order (oldest first within the window).</summary>
    Task<IReadOnlyList<ConversationMessage>> GetByThreadIdAsync(
        Guid threadId,
        int take,
        CancellationToken ct);
}
