using ArchLucid.Core.Conversation;
using ArchLucid.Persistence.Conversation;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Shared contract assertions for <see cref="IConversationThreadRepository" />.
/// </summary>
public abstract class ConversationThreadRepositoryContractTests
{
    private const int SeededThreadsForPagedScopeContract = 3;

    private static readonly Guid TenantId = Guid.Parse("c1c1c1c1-c1c1-c1c1-c1c1-c1c1c1c1c1c1");
    private static readonly Guid WorkspaceId = Guid.Parse("c2c2c2c2-c2c2-c2c2-c2c2-c2c2c2c2c2c2");
    private static readonly Guid ProjectId = Guid.Parse("c3c3c3c3-c3c3-c3c3-c3c3-c3c3c3c3c3c3");

    /// <summary>
    ///     <see cref="IConversationThreadRepository.ArchiveThreadsLastUpdatedBeforeAsync" /> touches all rows in SQL; shared
    ///     fixture is not isolated.
    /// </summary>
    protected virtual bool IncludeArchiveContractTest => true;

    protected abstract IConversationThreadRepository CreateRepository();

    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    private static ConversationThread NewThread(
        DateTime? lastUpdatedUtc = null,
        Guid? projectId = null)
    {
        DateTime stamp = lastUpdatedUtc ?? DateTime.UtcNow;

        return new ConversationThread
        {
            ThreadId = Guid.NewGuid(),
            TenantId = TenantId,
            WorkspaceId = WorkspaceId,
            ProjectId = projectId ?? ProjectId,
            Title = "contract-thread",
            CreatedUtc = stamp,
            LastUpdatedUtc = stamp
        };
    }

    [SkippableFact]
    public async Task Create_then_GetById_returns_same_thread()
    {
        SkipIfSqlServerUnavailable();
        IConversationThreadRepository repo = CreateRepository();
        ConversationThread thread = NewThread();

        await repo.CreateAsync(thread, CancellationToken.None);

        ConversationThread? loaded = await repo.GetByIdAsync(thread.ThreadId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.ThreadId.Should().Be(thread.ThreadId);
        loaded.Title.Should().Be(thread.Title);
        loaded.TenantId.Should().Be(TenantId);
    }

    [SkippableFact]
    public async Task GetById_nonexistent_returns_null()
    {
        SkipIfSqlServerUnavailable();
        IConversationThreadRepository repo = CreateRepository();

        ConversationThread? result = await repo.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    [SkippableFact]
    public async Task ListByScope_returns_only_matching_scope()
    {
        SkipIfSqlServerUnavailable();
        IConversationThreadRepository repo = CreateRepository();
        ConversationThread matching = NewThread();
        ConversationThread otherProject = NewThread(projectId: Guid.NewGuid());

        await repo.CreateAsync(matching, CancellationToken.None);
        await repo.CreateAsync(otherProject, CancellationToken.None);

        IReadOnlyList<ConversationThread> list = await repo.ListByScopeAsync(
            TenantId,
            WorkspaceId,
            ProjectId,
            50,
            CancellationToken.None);

        list.Should().Contain(t => t.ThreadId == matching.ThreadId);
        list.Should().NotContain(t => t.ThreadId == otherProject.ThreadId);
    }

    [SkippableFact]
    public async Task ListByScope_orders_by_LastUpdatedUtc_descending()
    {
        SkipIfSqlServerUnavailable();
        IConversationThreadRepository repo = CreateRepository();
        DateTime older = DateTime.UtcNow.AddMinutes(-30);
        DateTime newer = DateTime.UtcNow.AddMinutes(-10);
        ConversationThread first = NewThread(older);
        ConversationThread second = NewThread(newer);

        await repo.CreateAsync(first, CancellationToken.None);
        await repo.CreateAsync(second, CancellationToken.None);

        IReadOnlyList<ConversationThread> list = await repo.ListByScopeAsync(
            TenantId,
            WorkspaceId,
            ProjectId,
            10,
            CancellationToken.None);

        int i1 = list.ToList().FindIndex(t => t.ThreadId == first.ThreadId);
        int i2 = list.ToList().FindIndex(t => t.ThreadId == second.ThreadId);
        i1.Should().BeGreaterThan(i2);
    }

    [SkippableFact]
    public async Task ListByScopePaged_respects_skip_and_total()
    {
        SkipIfSqlServerUnavailable();
        IConversationThreadRepository repo = CreateRepository();

        for (int i = 0; i < SeededThreadsForPagedScopeContract; i++)
        {
            await repo.CreateAsync(NewThread(DateTime.UtcNow.AddSeconds(-i)), CancellationToken.None);
        }

        (IReadOnlyList<ConversationThread> page, int total) = await repo.ListByScopePagedAsync(
            TenantId,
            WorkspaceId,
            ProjectId,
            1,
            1,
            CancellationToken.None);

        total.Should().BeGreaterThanOrEqualTo(SeededThreadsForPagedScopeContract);
        page.Should().HaveCount(1);
    }

    [SkippableFact]
    public async Task UpdateLastUpdatedAsync_changes_GetById()
    {
        SkipIfSqlServerUnavailable();
        IConversationThreadRepository repo = CreateRepository();
        ConversationThread thread = NewThread();
        await repo.CreateAsync(thread, CancellationToken.None);

        DateTime updated = DateTime.UtcNow.AddHours(1);
        await repo.UpdateLastUpdatedAsync(thread.ThreadId, updated, CancellationToken.None);

        ConversationThread? loaded = await repo.GetByIdAsync(thread.ThreadId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.LastUpdatedUtc.Should().BeCloseTo(updated, TimeSpan.FromSeconds(2));
    }

    [SkippableFact]
    public async Task ArchiveThreadsLastUpdatedBefore_excludes_from_get()
    {
        Skip.IfNot(
            IncludeArchiveContractTest,
            "Shared SQL: ArchiveThreadsLastUpdatedBeforeAsync is global to conversation threads.");

        SkipIfSqlServerUnavailable();
        IConversationThreadRepository repo = CreateRepository();
        ConversationThread old = NewThread(DateTime.UtcNow.AddDays(-10));
        await repo.CreateAsync(old, CancellationToken.None);

        int n = await repo.ArchiveThreadsLastUpdatedBeforeAsync(
            DateTimeOffset.UtcNow.AddDays(-1),
            CancellationToken.None);

        n.Should().BeGreaterThan(0);

        ConversationThread? after = await repo.GetByIdAsync(old.ThreadId, CancellationToken.None);

        after.Should().BeNull();
    }
}
