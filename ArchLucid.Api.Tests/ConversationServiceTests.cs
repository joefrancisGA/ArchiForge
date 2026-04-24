using ArchLucid.Core.Conversation;
using ArchLucid.Host.Core.Ask;
using ArchLucid.Persistence.Conversation;

using FluentAssertions;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>
///     <see cref="ConversationService" /> thread lifecycle: get-or-create, append messages, history, last-updated bumps.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ConversationServiceTests
{
    private readonly Guid _project = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private readonly Guid _tenant = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _workspace = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task GetOrCreateThreadAsync_creates_new_thread_when_id_null()
    {
        Mock<IConversationThreadRepository> threads = new();
        Mock<IConversationMessageRepository> messages = new();

        threads
            .Setup(x => x.CreateAsync(It.IsAny<ConversationThread>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ConversationService sut = new(threads.Object, messages.Object);

        ConversationThread thread = await sut.GetOrCreateThreadAsync(
            null,
            _tenant,
            _workspace,
            _project,
            null,
            null,
            null,
            CancellationToken.None);

        thread.ThreadId.Should().NotBeEmpty();
        thread.TenantId.Should().Be(_tenant);
        thread.WorkspaceId.Should().Be(_workspace);
        thread.ProjectId.Should().Be(_project);

        threads.Verify(
            x => x.CreateAsync(It.Is<ConversationThread>(t => t.ThreadId == thread.ThreadId),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateThreadAsync_returns_existing_when_id_matches_scope()
    {
        Guid existingId = Guid.NewGuid();
        ConversationThread existing = new()
        {
            ThreadId = existingId,
            TenantId = _tenant,
            WorkspaceId = _workspace,
            ProjectId = _project,
            Title = "T",
            CreatedUtc = DateTime.UtcNow,
            LastUpdatedUtc = DateTime.UtcNow
        };

        Mock<IConversationThreadRepository> threads = new();
        Mock<IConversationMessageRepository> messages = new();

        threads
            .Setup(x => x.GetByIdAsync(existingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        ConversationService sut = new(threads.Object, messages.Object);

        ConversationThread result = await sut.GetOrCreateThreadAsync(
            existingId,
            _tenant,
            _workspace,
            _project,
            null,
            null,
            null,
            CancellationToken.None);

        result.Should().BeSameAs(existing);
        threads.Verify(x => x.CreateAsync(It.IsAny<ConversationThread>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreateThreadAsync_throws_when_thread_scope_mismatches()
    {
        Guid existingId = Guid.NewGuid();
        ConversationThread wrongScope = new()
        {
            ThreadId = existingId,
            TenantId = Guid.NewGuid(),
            WorkspaceId = _workspace,
            ProjectId = _project,
            Title = "T",
            CreatedUtc = DateTime.UtcNow,
            LastUpdatedUtc = DateTime.UtcNow
        };

        Mock<IConversationThreadRepository> threads = new();
        threads.Setup(x => x.GetByIdAsync(existingId, It.IsAny<CancellationToken>())).ReturnsAsync(wrongScope);

        ConversationService sut = new(threads.Object, Mock.Of<IConversationMessageRepository>());

        Func<Task> act = async () => await sut.GetOrCreateThreadAsync(
            existingId,
            _tenant,
            _workspace,
            _project,
            null,
            null,
            null,
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found for the current scope*");
    }

    [Fact]
    public async Task AppendUserMessageAsync_adds_message_and_updates_thread_timestamp()
    {
        Guid threadId = Guid.NewGuid();

        Mock<IConversationThreadRepository> threads = new();
        Mock<IConversationMessageRepository> messages = new();

        messages
            .Setup(x => x.AddAsync(It.IsAny<ConversationMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        threads
            .Setup(x => x.UpdateLastUpdatedAsync(threadId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ConversationService sut = new(threads.Object, messages.Object);

        await sut.AppendUserMessageAsync(threadId, "Hello", CancellationToken.None);

        messages.Verify(
            x => x.AddAsync(
                It.Is<ConversationMessage>(m =>
                    m.ThreadId == threadId &&
                    m.Role == ConversationMessageRole.User &&
                    m.Content == "Hello"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        threads.Verify(
            x => x.UpdateLastUpdatedAsync(threadId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AppendAssistantMessageAsync_adds_message_with_metadata_and_updates_thread()
    {
        Guid threadId = Guid.NewGuid();

        Mock<IConversationThreadRepository> threads = new();
        Mock<IConversationMessageRepository> messages = new();

        messages
            .Setup(x => x.AddAsync(It.IsAny<ConversationMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        threads
            .Setup(x => x.UpdateLastUpdatedAsync(threadId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ConversationService sut = new(threads.Object, messages.Object);

        await sut.AppendAssistantMessageAsync(threadId, "Reply body", """{"citations":[]}""", CancellationToken.None);

        messages.Verify(
            x => x.AddAsync(
                It.Is<ConversationMessage>(m =>
                    m.ThreadId == threadId &&
                    m.Role == ConversationMessageRole.Assistant &&
                    m.Content == "Reply body" &&
                    m.MetadataJson.Contains("citations", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()),
            Times.Once);

        threads.Verify(
            x => x.UpdateLastUpdatedAsync(threadId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetHistoryAsync_delegates_to_repository()
    {
        Guid threadId = Guid.NewGuid();
        List<ConversationMessage> expected =
        [
            new()
            {
                MessageId = Guid.NewGuid(), ThreadId = threadId, Role = ConversationMessageRole.User, Content = "x"
            }
        ];

        Mock<IConversationMessageRepository> messages = new();
        messages
            .Setup(x => x.GetByThreadIdAsync(threadId, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        ConversationService sut = new(Mock.Of<IConversationThreadRepository>(), messages.Object);

        IReadOnlyList<ConversationMessage> history = await sut.GetHistoryAsync(threadId, 25, CancellationToken.None);

        history.Should().BeEquivalentTo(expected);
    }
}
