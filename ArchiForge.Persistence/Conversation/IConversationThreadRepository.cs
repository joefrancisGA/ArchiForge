using ArchiForge.Core.Conversation;

namespace ArchiForge.Persistence.Conversation;

public interface IConversationThreadRepository
{
    Task CreateAsync(ConversationThread thread, CancellationToken ct);

    Task<ConversationThread?> GetByIdAsync(Guid threadId, CancellationToken ct);

    Task<IReadOnlyList<ConversationThread>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        int take,
        CancellationToken ct);

    Task UpdateLastUpdatedAsync(Guid threadId, DateTime updatedUtc, CancellationToken ct);
}
