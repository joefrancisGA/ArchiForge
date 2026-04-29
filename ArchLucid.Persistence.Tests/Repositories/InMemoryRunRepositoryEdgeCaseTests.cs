using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Repositories;
using ArchLucid.Persistence.Tenancy;

namespace ArchLucid.Persistence.Tests.Repositories;

/// <summary>
///     Additional branch coverage for <see cref="InMemoryRunRepository" /> beyond shared contract tests (archive-by-id
///     failures, update concurrency, empty archive-by-id).
/// </summary>
[Trait("Category", "Unit")]
public sealed class InMemoryRunRepositoryEdgeCaseTests
{
    private static ScopeContext NewScope()
    {
        return new ScopeContext { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() };
    }

    private static RunRecord NewRun(ScopeContext scope, DateTime createdUtc)
    {
        return new RunRecord
        {
            RunId = Guid.NewGuid(),
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ScopeProjectId = scope.ProjectId,
            ProjectId = "edge-case-proj",
            Description = "edge",
            CreatedUtc = createdUtc
        };
    }

    [Fact]
    public async Task UpdateAsync_when_run_missing_throws_invalid_operation_exception()
    {
        IRunRepository repo = new InMemoryRunRepository(new InMemoryTenantRepository());
        ScopeContext scope = NewScope();
        RunRecord run = NewRun(scope, DateTime.UtcNow);

        Func<Task> act = async () => await repo.UpdateAsync(run, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Run '{run.RunId:D}' was not found for update.");
    }

    [Fact]
    public async Task UpdateAsync_when_row_version_mismatch_throws_run_concurrency_conflict_exception()
    {
        IRunRepository repo = new InMemoryRunRepository(new InMemoryTenantRepository());
        ScopeContext scope = NewScope();
        RunRecord run = NewRun(scope, DateTime.UtcNow);

        await repo.SaveAsync(run, CancellationToken.None);

        RunRecord? loaded = await repo.GetByIdAsync(scope, run.RunId, CancellationToken.None);
        loaded.Should().NotBeNull();
        RunRecord stale = loaded;
        stale.RowVersion = [1, 2, 3, 4, 5, 6, 7, 8];

        Func<Task> act = async () => await repo.UpdateAsync(stale, CancellationToken.None);

        await act.Should().ThrowAsync<RunConcurrencyConflictException>();
    }

    [Fact]
    public async Task ArchiveRunsByIdsAsync_empty_returns_empty_result()
    {
        IRunRepository repo = new InMemoryRunRepository(new InMemoryTenantRepository());

        RunArchiveByIdsResult result = await repo.ArchiveRunsByIdsAsync([], CancellationToken.None);

        result.SucceededRunIds.Should().BeEmpty();
        result.ArchivedRuns.Should().BeEmpty();
        result.Failed.Should().BeEmpty();
    }

    [Fact]
    public async Task ArchiveRunsByIdsAsync_duplicate_ids_in_request_archives_once()
    {
        IRunRepository repo = new InMemoryRunRepository(new InMemoryTenantRepository());
        ScopeContext scope = NewScope();
        RunRecord run = NewRun(scope, DateTime.UtcNow);

        await repo.SaveAsync(run, CancellationToken.None);

        RunArchiveByIdsResult result =
            await repo.ArchiveRunsByIdsAsync([run.RunId, run.RunId], CancellationToken.None);

        result.SucceededRunIds.Should().ContainSingle().Which.Should().Be(run.RunId);
        result.Failed.Should().BeEmpty();
    }

    [Fact]
    public async Task ArchiveRunsByIdsAsync_unknown_id_records_failure()
    {
        IRunRepository repo = new InMemoryRunRepository(new InMemoryTenantRepository());
        Guid missing = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        RunArchiveByIdsResult result = await repo.ArchiveRunsByIdsAsync([missing], CancellationToken.None);

        result.SucceededRunIds.Should().BeEmpty();
        result.Failed.Should().ContainSingle();
        result.Failed[0].RunId.Should().Be(missing);
        result.Failed[0].Reason.Should().Be("Run not found.");
    }
}
