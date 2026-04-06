using ArchiForge.Core.Conversation;
using ArchiForge.Persistence.Conversation;

namespace ArchiForge.Host.Core.Ask;

/// <summary>
/// <see cref="IConversationService"/> implementation backed by thread and message repositories.
/// </summary>
public sealed class ConversationService(
    IConversationThreadRepository threadRepository,
    IConversationMessageRepository messageRepository) : IConversationService
{
    /// <inheritdoc />
    public async Task<ConversationThread> GetOrCreateThreadAsync(
        Guid? threadId,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid? runId,
        Guid? baseRunId,
        Guid? targetRunId,
        CancellationToken ct)
    {
        if (threadId.HasValue)
        {
            ConversationThread? existing = await threadRepository.GetByIdAsync(threadId.Value, ct);
            if (existing is not null)
            {
                if (existing.TenantId != tenantId ||
                    existing.WorkspaceId != workspaceId ||
                    existing.ProjectId != projectId)
                
                    throw new InvalidOperationException("Conversation thread not found for the current scope.");
                

                return existing;
            }
        }

        ConversationThread thread = new()
        {
            ThreadId = Guid.NewGuid(),
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            RunId = runId,
            BaseRunId = baseRunId,
            TargetRunId = targetRunId,
            Title = "Architecture Conversation",
            CreatedUtc = DateTime.UtcNow,
            LastUpdatedUtc = DateTime.UtcNow
        };

        await threadRepository.CreateAsync(thread, ct);
        return thread;
    }

    public Task<IReadOnlyList<ConversationMessage>> GetHistoryAsync(Guid threadId, int take, CancellationToken ct) =>
        messageRepository.GetByThreadIdAsync(threadId, take, ct);

    public async Task AppendUserMessageAsync(Guid threadId, string content, CancellationToken ct)
    {
        await messageRepository.AddAsync(
            new ConversationMessage
            {
                MessageId = Guid.NewGuid(),
                ThreadId = threadId,
                Role = ConversationMessageRole.User,
                Content = content,
                CreatedUtc = DateTime.UtcNow,
                MetadataJson = "{}"
            },
            ct);

        await threadRepository.UpdateLastUpdatedAsync(threadId, DateTime.UtcNow, ct);
    }

    /// <inheritdoc />
    public async Task AppendAssistantMessageAsync(
        Guid threadId,
        string content,
        string metadataJson,
        CancellationToken ct)
    {
        await messageRepository.AddAsync(
            new ConversationMessage
            {
                MessageId = Guid.NewGuid(),
                ThreadId = threadId,
                Role = ConversationMessageRole.Assistant,
                Content = content,
                CreatedUtc = DateTime.UtcNow,
                MetadataJson = metadataJson
            },
            ct);

        await threadRepository.UpdateLastUpdatedAsync(threadId, DateTime.UtcNow, ct);
    }
}
