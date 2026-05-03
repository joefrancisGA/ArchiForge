using ArchLucid.Contracts.Common;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Repositories;
using ArchLucid.Persistence.Tenancy;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.Repositories;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class RunRepositoryCommittedArchitectureReviewFlagReaderTests
{
    [SkippableFact]
    public async Task Returns_false_when_no_committed_manifest_run()
    {
        InMemoryRunRepository runs = new(new InMemoryTenantRepository());
        RunRepositoryCommittedArchitectureReviewFlagReader sut = new(runs);
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
        };

        RunRecord open = BuildRun(scope, nameof(ArchitectureRunStatus.ReadyForCommit), null);
        await runs.SaveAsync(open, CancellationToken.None);

        bool result = await sut.TenantHasCommittedArchitectureReviewAsync(scope, CancellationToken.None);

        result.Should().BeFalse();
    }

    [SkippableFact]
    public async Task Returns_false_when_committed_but_no_golden_manifest()
    {
        InMemoryRunRepository runs = new(new InMemoryTenantRepository());
        RunRepositoryCommittedArchitectureReviewFlagReader sut = new(runs);
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
        };

        RunRecord committedWithoutGolden = BuildRun(scope, nameof(ArchitectureRunStatus.Committed), null);

        await runs.SaveAsync(committedWithoutGolden, CancellationToken.None);

        bool result = await sut.TenantHasCommittedArchitectureReviewAsync(scope, CancellationToken.None);

        result.Should().BeFalse();
    }

    [SkippableFact]
    public async Task Returns_true_when_committed_with_golden_manifest()
    {
        InMemoryRunRepository runs = new(new InMemoryTenantRepository());
        RunRepositoryCommittedArchitectureReviewFlagReader sut = new(runs);
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
        };

        RunRecord committed = BuildRun(scope, nameof(ArchitectureRunStatus.Committed), Guid.NewGuid());

        await runs.SaveAsync(committed, CancellationToken.None);

        bool result = await sut.TenantHasCommittedArchitectureReviewAsync(scope, CancellationToken.None);

        result.Should().BeTrue();
    }

    private static RunRecord BuildRun(ScopeContext scope, string legacyStatus, Guid? goldenManifestId)
    {
        return new RunRecord
        {
            RunId = Guid.NewGuid(),
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ScopeProjectId = scope.ProjectId,
            ProjectId = "p",
            LegacyRunStatus = legacyStatus,
            GoldenManifestId = goldenManifestId,
            CreatedUtc = DateTime.UtcNow,
        };
    }
}
