using ArchiForge.Core.Conversation;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Advisory.Scheduling;
using ArchiForge.Persistence.Advisory;
using ArchiForge.Persistence.Archival;
using ArchiForge.Persistence.Conversation;
using ArchiForge.Persistence.Interfaces;
using ArchiForge.Persistence.Models;
using ArchiForge.Persistence.Repositories;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

namespace ArchiForge.Persistence.Tests;

public sealed class DataArchivalCoordinatorTests
{
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
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid()
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
            RunsRetentionDays = 30,
            DigestsRetentionDays = 30,
            ConversationsRetentionDays = 30
        };

        await coordinator.RunOnceAsync(options, CancellationToken.None);

        IReadOnlyList<RunRecord> listed =
            await runs.ListByProjectAsync(scope, "p", 10, CancellationToken.None);
        listed.Should().BeEmpty();

        IReadOnlyList<ArchitectureDigest> digestList =
            await digests.ListByScopeAsync(scope.TenantId, scope.WorkspaceId, scope.ProjectId, 10, CancellationToken.None);
        digestList.Should().BeEmpty();

        IReadOnlyList<ConversationThread> threadList =
            await threads.ListByScopeAsync(scope.TenantId, scope.WorkspaceId, scope.ProjectId, 10, CancellationToken.None);
        threadList.Should().BeEmpty();
    }
}
