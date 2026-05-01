using ArchLucid.Core.Conversation;
using ArchLucid.Persistence.Conversation;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Shared contract assertions for <see cref="IConversationMessageRepository" />.
/// </summary>
public abstract class ConversationMessageRepositoryContractTests
{
    private const int SeededMessagesForThreadTakeContract = 5;
    protected abstract IConversationMessageRepository CreateRepository();

    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    /// <summary>SQL needs a parent thread row before inserting messages.</summary>
    protected virtual Task EnsureThreadExistsAsync(ConversationThread thread)
    {
        return Task.CompletedTask;
    }

    private static ConversationThread NewThreadForMessages()
    {
        return new ConversationThread
        {
            ThreadId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Title = "msg-contract",
            CreatedUtc = DateTime.UtcNow,
            LastUpdatedUtc = DateTime.UtcNow
        };
    }

    [SkippableFact]
    public async Task Add_then_GetByThreadId_returns_oldest_first_within_window()
    {
        SkipIfSqlServerUnavailable();
        IConversationMessageRepository repo = CreateRepository();
        ConversationThread thread = NewThreadForMessages();
        await EnsureThreadExistsAsync(thread);

        ConversationMessage first = new()
        {
            MessageId = Guid.NewGuid(),
            ThreadId = thread.ThreadId,
            Role = ConversationMessageRole.User,
            Content = "a",
            CreatedUtc = DateTime.UtcNow.AddMinutes(-5)
        };

        ConversationMessage second = new()
        {
            MessageId = Guid.NewGuid(),
            ThreadId = thread.ThreadId,
            Role = ConversationMessageRole.Assistant,
            Content = "b",
            CreatedUtc = DateTime.UtcNow.AddMinutes(-4)
        };

        await repo.AddAsync(first, CancellationToken.None);
        await repo.AddAsync(second, CancellationToken.None);

        IReadOnlyList<ConversationMessage> window =
            await repo.GetByThreadIdAsync(thread.ThreadId, 10, CancellationToken.None);

        window.Should().HaveCount(2);
        window[0].MessageId.Should().Be(first.MessageId);
        window[1].MessageId.Should().Be(second.MessageId);
    }

    [SkippableFact]
    public async Task GetByThreadId_respects_take()
    {
        SkipIfSqlServerUnavailable();
        IConversationMessageRepository repo = CreateRepository();
        ConversationThread thread = NewThreadForMessages();
        await EnsureThreadExistsAsync(thread);

        for (int i = 0; i < SeededMessagesForThreadTakeContract; i++)
        {
            await repo.AddAsync(
                new ConversationMessage
                {
                    MessageId = Guid.NewGuid(),
                    ThreadId = thread.ThreadId,
                    Role = ConversationMessageRole.User,
                    Content = $"m{i}",
                    CreatedUtc = DateTime.UtcNow.AddSeconds(-i)
                },
                CancellationToken.None);
        }

        IReadOnlyList<ConversationMessage> window =
            await repo.GetByThreadIdAsync(thread.ThreadId, 2, CancellationToken.None);

        window.Should().HaveCount(2);
    }
}
