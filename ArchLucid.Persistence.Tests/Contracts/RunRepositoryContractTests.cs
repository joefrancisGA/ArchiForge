using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
/// Shared contract assertions for <see cref="IRunRepository"/>.
/// </summary>
public abstract class RunRepositoryContractTests
{
    /// <summary>No-op for in-memory; Dapper + SQL subclasses skip when SQL is unavailable.</summary>
    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    /// <summary>
    /// <see cref="IRunRepository.ArchiveRunsCreatedBeforeAsync"/> updates all matching rows in SQL; the shared container
    /// fixture is not isolated per test, so only the in-memory implementation runs this case.
    /// </summary>
    protected virtual bool IncludeArchiveRunsCreatedBeforeContractTest => true;

    protected abstract IRunRepository CreateRepository();

    private static ScopeContext NewScope() =>
        new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid()
        };

    private static RunRecord NewRun(ScopeContext scope, string projectSlug, DateTime createdUtc)
    {
        return new RunRecord
        {
            RunId = Guid.NewGuid(),
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ScopeProjectId = scope.ProjectId,
            ProjectId = projectSlug,
            Description = "contract",
            CreatedUtc = createdUtc
        };
    }

    [SkippableFact]
    public async Task Save_then_GetById_returns_same_record()
    {
        SkipIfSqlServerUnavailable();
        IRunRepository repo = CreateRepository();
        ScopeContext scope = NewScope();
        RunRecord run = NewRun(scope, "proj_a", DateTime.UtcNow);

        await repo.SaveAsync(run, CancellationToken.None);

        RunRecord? loaded = await repo.GetByIdAsync(scope, run.RunId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.RunId.Should().Be(run.RunId);
        loaded.ProjectId.Should().Be(run.ProjectId);
        loaded.TenantId.Should().Be(scope.TenantId);
    }

    [SkippableFact]
    public async Task GetById_wrong_scope_returns_null()
    {
        SkipIfSqlServerUnavailable();
        IRunRepository repo = CreateRepository();
        ScopeContext scope = NewScope();
        RunRecord run = NewRun(scope, "proj_a", DateTime.UtcNow);

        await repo.SaveAsync(run, CancellationToken.None);

        ScopeContext otherTenant = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId
        };

        RunRecord? loaded = await repo.GetByIdAsync(otherTenant, run.RunId, CancellationToken.None);

        loaded.Should().BeNull();
    }

    [SkippableFact]
    public async Task ListByProjectAsync_orders_newest_first_and_respects_take()
    {
        SkipIfSqlServerUnavailable();
        IRunRepository repo = CreateRepository();
        ScopeContext scope = NewScope();
        string slug = "list_proj_" + Guid.NewGuid().ToString("N");
        DateTime older = DateTime.UtcNow.AddMinutes(-10);
        DateTime newer = DateTime.UtcNow.AddMinutes(-5);

        RunRecord first = NewRun(scope, slug, older);
        RunRecord second = NewRun(scope, slug, newer);

        await repo.SaveAsync(first, CancellationToken.None);
        await repo.SaveAsync(second, CancellationToken.None);

        IReadOnlyList<RunRecord> list = await repo.ListByProjectAsync(scope, slug, take: 1, CancellationToken.None);

        list.Should().HaveCount(1);
        list[0].RunId.Should().Be(second.RunId);
    }

    [SkippableFact]
    public async Task ListByProjectPagedAsync_returns_total_and_page_slice_newest_first()
    {
        SkipIfSqlServerUnavailable();
        IRunRepository repo = CreateRepository();
        ScopeContext scope = NewScope();
        string slug = "paged_proj_" + Guid.NewGuid().ToString("N");
        DateTime t0 = DateTime.UtcNow.AddMinutes(-30);
        DateTime t1 = DateTime.UtcNow.AddMinutes(-20);
        DateTime t2 = DateTime.UtcNow.AddMinutes(-10);

        RunRecord a = NewRun(scope, slug, t0);
        RunRecord b = NewRun(scope, slug, t1);
        RunRecord c = NewRun(scope, slug, t2);

        await repo.SaveAsync(a, CancellationToken.None);
        await repo.SaveAsync(b, CancellationToken.None);
        await repo.SaveAsync(c, CancellationToken.None);

        (IReadOnlyList<RunRecord> page1, int total1) =
            await repo.ListByProjectPagedAsync(scope, slug, skip: 0, take: 2, CancellationToken.None);

        total1.Should().Be(3);
        page1.Should().HaveCount(2);
        page1[0].RunId.Should().Be(c.RunId);
        page1[1].RunId.Should().Be(b.RunId);

        (IReadOnlyList<RunRecord> page2, int total2) =
            await repo.ListByProjectPagedAsync(scope, slug, skip: 2, take: 2, CancellationToken.None);

        total2.Should().Be(3);
        page2.Should().HaveCount(1);
        page2[0].RunId.Should().Be(a.RunId);
    }

    [SkippableFact]
    public async Task UpdateAsync_changes_fields_visible_on_GetById()
    {
        SkipIfSqlServerUnavailable();
        IRunRepository repo = CreateRepository();
        ScopeContext scope = NewScope();
        RunRecord run = NewRun(scope, "proj_u", DateTime.UtcNow);

        await repo.SaveAsync(run, CancellationToken.None);

        run.Description = "updated";

        await repo.UpdateAsync(run, CancellationToken.None);

        RunRecord? loaded = await repo.GetByIdAsync(scope, run.RunId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.Description.Should().Be("updated");
    }

    [SkippableFact]
    public async Task ArchiveRunsCreatedBefore_async_marks_rows_and_excludes_from_get()
    {
        Skip.IfNot(
            IncludeArchiveRunsCreatedBeforeContractTest,
            "Shared SQL: ArchiveRunsCreatedBeforeAsync is global to dbo.Runs.");

        SkipIfSqlServerUnavailable();
        IRunRepository repo = CreateRepository();
        ScopeContext scope = NewScope();
        RunRecord oldRun = NewRun(scope, "proj_arch", DateTime.UtcNow.AddDays(-5));

        await repo.SaveAsync(oldRun, CancellationToken.None);

        RunArchiveBatchResult batch =
            await repo.ArchiveRunsCreatedBeforeAsync(DateTimeOffset.UtcNow.AddDays(-1), CancellationToken.None);

        batch.UpdatedCount.Should().Be(1);

        RunRecord? after = await repo.GetByIdAsync(scope, oldRun.RunId, CancellationToken.None);

        after.Should().BeNull();
    }
}
