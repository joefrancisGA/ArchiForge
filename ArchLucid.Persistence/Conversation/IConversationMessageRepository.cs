using ArchiForge.Core.Conversation;

namespace ArchiForge.Persistence.Conversation;

/// <summary>
/// Persistence for <see cref="ConversationMessage"/> rows used by Ask.
/// </summary>
/// <remarks>
/// SQL: <see cref="DapperConversationMessageRepository"/>; in-memory: <see cref="InMemoryConversationMessageRepository"/>.
/// Primary caller: <c>ArchiForge.Api.Ask.ConversationService</c>.
/// </remarks>
public interface IConversationMessageRepository
{
    /// <summary>Appends a message row.</summary>
    Task AddAsync(ConversationMessage message, CancellationToken ct);

    /// <summary>Most recent <paramref name="take"/> messages in chronological order (oldest first within the window).</summary>
    Task<IReadOnlyList<ConversationMessage>> GetByThreadIdAsync(
        Guid threadId,
        int take,
        CancellationToken ct);
}
