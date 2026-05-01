using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Shared contract assertions for <see cref="IRunRepository" />.
/// </summary>
public abstract class RunRepositoryContractTests
{
    /// <summary>
    ///     <see cref="IRunRepository.ArchiveRunsCreatedBeforeAsync" /> updates all matching rows in SQL; the shared container
    ///     fixture is not isolated per test, so only the in-memory implementation runs this case.
    /// </summary>
    protected virtual bool IncludeArchiveRunsCreatedBeforeContractTest => true;

    /// <summary>
    ///     <see cref="IRunRepository.ArchiveRunsByIdsAsync" /> touches global <c>dbo.Runs</c> in SQL; only the in-memory
    ///     implementation runs this case against the shared container.
    /// </summary>
    protected virtual bool IncludeArchiveRunsByIdsContractTest => true;

    /// <summary>No-op for in-memory; Dapper + SQL subclasses skip when SQL is unavailable.</summary>
    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    protected abstract IRunRepository CreateRepository();

    private static ScopeContext NewScope()
    {
        return new ScopeContext { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() };
    }

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
            TenantId = Guid.NewGuid(), WorkspaceId = scope.WorkspaceId, ProjectId = scope.ProjectId
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

        IReadOnlyList<RunRecord> list = await repo.ListByProjectAsync(scope, slug, 1, CancellationToken.None);

        list.Should().HaveCount(1);
        list[0].RunId.Should().Be(second.RunId);
    }

    [SkippableFact]
    public async Task ListByProjectKeysetAsync_returns_slices_newest_first()
    {
        SkipIfSqlServerUnavailable();
        IRunRepository repo = CreateRepository();
        ScopeContext scope = NewScope();
        string slug = "keyset_proj_" + Guid.NewGuid().ToString("N");
        DateTime t0 = DateTime.UtcNow.AddMinutes(-30);
        DateTime t1 = DateTime.UtcNow.AddMinutes(-20);
        DateTime t2 = DateTime.UtcNow.AddMinutes(-10);

        RunRecord a = NewRun(scope, slug, t0);
        RunRecord b = NewRun(scope, slug, t1);
        RunRecord c = NewRun(scope, slug, t2);

        await repo.SaveAsync(a, CancellationToken.None);
        await repo.SaveAsync(b, CancellationToken.None);
        await repo.SaveAsync(c, CancellationToken.None);

        RunListPage page1 =
            await repo.ListByProjectKeysetAsync(scope, slug, null, null, 2, CancellationToken.None);

        page1.HasMore.Should().BeTrue();
        page1.Items.Should().HaveCount(2);
        page1.Items[0].RunId.Should().Be(c.RunId);
        page1.Items[1].RunId.Should().Be(b.RunId);

        RunRecord cursorRow = page1.Items[1];
        RunListPage page2 =
            await repo.ListByProjectKeysetAsync(scope, slug, cursorRow.CreatedUtc, cursorRow.RunId, 2,

                CancellationToken.None);

        page2.HasMore.Should().BeFalse();
        page2.Items.Should().HaveCount(1);
        page2.Items[0].RunId.Should().Be(a.RunId);
    }

    [SkippableFact]
    public async Task ListRecentInScopeAsync_orders_newest_first_and_spans_project_slugs()
    {
        SkipIfSqlServerUnavailable();
        IRunRepository repo = CreateRepository();
        ScopeContext scope = NewScope();
        DateTime older = DateTime.UtcNow.AddMinutes(-10);
        DateTime newer = DateTime.UtcNow.AddMinutes(-5);

        RunRecord first = NewRun(scope, "slug_a", older);
        RunRecord second = NewRun(scope, "slug_b", newer);

        await repo.SaveAsync(first, CancellationToken.None);
        await repo.SaveAsync(second, CancellationToken.None);

        IReadOnlyList<RunRecord> list = await repo.ListRecentInScopeAsync(scope, 10, CancellationToken.None);

        list.Should().HaveCount(2);
        list[0].RunId.Should().Be(second.RunId);
        list[1].RunId.Should().Be(first.RunId);
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

    [SkippableFact]
    public async Task ArchiveRunsByIds_async_archives_only_requested_rows_and_classifies_failures()
    {
        Skip.IfNot(
            IncludeArchiveRunsByIdsContractTest,
            "Shared SQL: ArchiveRunsByIdsAsync is not isolated per test on dbo.Runs.");

        SkipIfSqlServerUnavailable();
        IRunRepository repo = CreateRepository();
        ScopeContext scope = NewScope();
        RunRecord a = NewRun(scope, "proj_ids", DateTime.UtcNow);
        RunRecord b = NewRun(scope, "proj_ids", DateTime.UtcNow);
        Guid missing = Guid.NewGuid();

        await repo.SaveAsync(a, CancellationToken.None);
        await repo.SaveAsync(b, CancellationToken.None);

        RunArchiveByIdsResult first =
            await repo.ArchiveRunsByIdsAsync([a.RunId, missing, a.RunId], CancellationToken.None);

        first.SucceededRunIds.Should().ContainSingle().Which.Should().Be(a.RunId);
        first.Failed.Should().HaveCount(1);
        first.Failed[0].RunId.Should().Be(missing);
        first.Failed[0].Reason.Should().Be("Run not found.");

        RunRecord? aAfter = await repo.GetByIdAsync(scope, a.RunId, CancellationToken.None);

        aAfter.Should().BeNull();

        RunArchiveByIdsResult second = await repo.ArchiveRunsByIdsAsync([a.RunId, b.RunId], CancellationToken.None);

        second.SucceededRunIds.Should().ContainSingle().Which.Should().Be(b.RunId);
        second.Failed.Should().ContainSingle();
        second.Failed[0].RunId.Should().Be(a.RunId);
        second.Failed[0].Reason.Should().Be("Run already archived.");
    }
}
