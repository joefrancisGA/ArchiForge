using ArchLucid.Core.Conversation;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Advisory.Scheduling;
using ArchLucid.Persistence.Archival;
using ArchLucid.Persistence.Conversation;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Repositories;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

namespace ArchLucid.Persistence.Tests;

[Collection(nameof(DataArchivalCoordinatorCollection))]
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class DataArchivalCoordinatorTests
{
    [Fact]
    public async Task RunOnceAsync_when_all_retention_non_positive_skips_archival_paths()
    {
        InMemoryRunRepository runs = new();
        InMemoryArchitectureDigestRepository digests = new();
        InMemoryConversationThreadRepository threads = new();
        DataArchivalCoordinator coordinator = new(
            runs,
            digests,
            threads,
            NullLogger<DataArchivalCoordinator>.Instance);

        DateTime old = DateTime.UtcNow.AddDays(-400);
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid()
        };

        await runs.SaveAsync(
            new RunRecord
            {
                RunId = Guid.NewGuid(),
                TenantId = scope.TenantId,
                WorkspaceId = scope.WorkspaceId,
                ScopeProjectId = scope.ProjectId,
                ProjectId = "p",
                CreatedUtc = old
            },
            CancellationToken.None);

        DataArchivalOptions options = new()
        {
            RunsRetentionDays = 0, DigestsRetentionDays = 0, ConversationsRetentionDays = 0
        };

        await coordinator.RunOnceAsync(options, CancellationToken.None);

        IReadOnlyList<RunRecord> listed =
            await runs.ListByProjectAsync(scope, "p", 10, CancellationToken.None);
        listed.Should().ContainSingle();
        listed[0].ArchivedUtc.Should().BeNull();
    }

    [Fact]
    public async Task RunOnceAsync_ArchivesRunsDigestsAndThreads_ByRetention()
    {
        InMemoryRunRepository runs = new();
        InMemoryArchitectureDigestRepository digests = new();
        InMemoryConversationThreadRepository threads = new();
        DataArchivalCoordinator coordinator = new(
            runs,
            digests,
            threads,
            NullLogger<DataArchivalCoordinator>.Instance);

        DateTime old = DateTime.UtcNow.AddDays(-400);
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid()
        };

        await runs.SaveAsync(
            new RunRecord
            {
                RunId = Guid.NewGuid(),
                TenantId = scope.TenantId,
                WorkspaceId = scope.WorkspaceId,
                ScopeProjectId = scope.ProjectId,
                ProjectId = "p",
                CreatedUtc = old
            },
            CancellationToken.None);

        ArchitectureDigest digest = new()
        {
            DigestId = Guid.NewGuid(),
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId,
            Title = "t",
            Summary = "s",
            ContentMarkdown = "# x",
            GeneratedUtc = old
        };

        await digests.CreateAsync(digest, CancellationToken.None);

        ConversationThread thread = new()
        {
            ThreadId = Guid.NewGuid(),
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId,
            LastUpdatedUtc = old
        };

        await threads.CreateAsync(thread, CancellationToken.None);

        DataArchivalOptions options = new()
        {
            RunsRetentionDays = 30, DigestsRetentionDays = 30, ConversationsRetentionDays = 30
        };

        await coordinator.RunOnceAsync(options, CancellationToken.None);

        IReadOnlyList<RunRecord> listed =
            await runs.ListByProjectAsync(scope, "p", 10, CancellationToken.None);
        listed.Should().BeEmpty();

        IReadOnlyList<ArchitectureDigest> digestList =
            await digests.ListByScopeAsync(scope.TenantId, scope.WorkspaceId, scope.ProjectId, 10,
                CancellationToken.None);
        digestList.Should().BeEmpty();

        IReadOnlyList<ConversationThread> threadList =
            await threads.ListByScopeAsync(scope.TenantId, scope.WorkspaceId, scope.ProjectId, 10,
                CancellationToken.None);
        threadList.Should().BeEmpty();
    }
}
