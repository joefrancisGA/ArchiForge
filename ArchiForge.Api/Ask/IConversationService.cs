using ArchiForge.Core.Conversation;

namespace ArchiForge.Api.Ask;

public interface IConversationService
{
    Task<ConversationThread> GetOrCreateThreadAsync(
        Guid? threadId,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid? runId,
        Guid? baseRunId,
        Guid? targetRunId,
        CancellationToken ct);

    Task<IReadOnlyList<ConversationMessage>> GetHistoryAsync(Guid threadId, int take, CancellationToken ct);

    Task AppendUserMessageAsync(Guid threadId, string content, CancellationToken ct);

    Task AppendAssistantMessageAsync(Guid threadId, string content, string metadataJson, CancellationToken ct);
}
