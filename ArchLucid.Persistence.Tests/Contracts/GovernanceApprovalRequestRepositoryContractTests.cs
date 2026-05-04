using ArchLucid.Contracts.Governance;
using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Shared contract assertions for <see cref="IGovernanceApprovalRequestRepository" />.
/// </summary>
public abstract class GovernanceApprovalRequestRepositoryContractTests
{
    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    protected abstract IGovernanceApprovalRequestRepository CreateRepository();

    [SkippableFact]
    public async Task Create_then_GetById_round_trips()
    {
        SkipIfSqlServerUnavailable();
        IGovernanceApprovalRequestRepository repo = CreateRepository();
        string runId = Guid.NewGuid().ToString("N");
        string approvalId = "apr-" + Guid.NewGuid().ToString("N");
        DateTime requestedUtc = new(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);

        GovernanceApprovalRequest item = NewApproval(approvalId, runId, requestedUtc);

        await repo.CreateAsync(item, CancellationToken.None);

        GovernanceApprovalRequest? loaded = await repo.GetByIdAsync(approvalId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.ApprovalRequestId.Should().Be(approvalId);
        loaded.RunId.Should().Be(runId);
        loaded.Status.Should().Be(GovernanceApprovalStatus.Submitted);
    }

    [SkippableFact]
    public async Task Update_then_GetById_reflects_status()
    {
        SkipIfSqlServerUnavailable();
        IGovernanceApprovalRequestRepository repo = CreateRepository();
        string runId = Guid.NewGuid().ToString("N");
        string approvalId = "apr-upd-" + Guid.NewGuid().ToString("N");
        GovernanceApprovalRequest item = NewApproval(approvalId, runId, DateTime.UtcNow);

        await repo.CreateAsync(item, CancellationToken.None);

        item.Status = GovernanceApprovalStatus.Approved;
        item.ReviewedBy = "reviewer";
        item.ReviewedUtc = new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc);

        await repo.UpdateAsync(item, CancellationToken.None);

        GovernanceApprovalRequest? loaded = await repo.GetByIdAsync(approvalId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.Status.Should().Be(GovernanceApprovalStatus.Approved);
        loaded.ReviewedBy.Should().Be("reviewer");
        loaded.ReviewedUtc.Should().Be(new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc));
    }

    [SkippableFact]
    public async Task TryTransitionFromReviewableAsync_second_approve_returns_false()
    {
        SkipIfSqlServerUnavailable();
        IGovernanceApprovalRequestRepository repo = CreateRepository();
        string runId = Guid.NewGuid().ToString("N");
        string approvalId = "apr-try-" + Guid.NewGuid().ToString("N");
        GovernanceApprovalRequest item = NewApproval(approvalId, runId, DateTime.UtcNow);

        await repo.CreateAsync(item, CancellationToken.None);

        DateTime reviewedUtc = new(2026, 4, 3, 12, 0, 0, DateTimeKind.Utc);

        bool first = await repo.TryTransitionFromReviewableAsync(
            approvalId,
            GovernanceApprovalStatus.Approved,
            "r1",
            "r1",
            "ok",
            reviewedUtc,
            CancellationToken.None);

        bool second = await repo.TryTransitionFromReviewableAsync(
            approvalId,
            GovernanceApprovalStatus.Approved,
            "r2",
            "r2",
            "dup",
            reviewedUtc.AddMinutes(1),
            CancellationToken.None);

        first.Should().BeTrue();
        second.Should().BeFalse();

        GovernanceApprovalRequest? loaded = await repo.GetByIdAsync(approvalId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.Status.Should().Be(GovernanceApprovalStatus.Approved);
        loaded.ReviewedBy.Should().Be("r1");
        loaded.ReviewComment.Should().Be("ok");
    }

    [SkippableFact]
    public async Task TryTransitionFromReviewableAsync_parallel_approves_only_one_wins()
    {
        SkipIfSqlServerUnavailable();
        IGovernanceApprovalRequestRepository repo = CreateRepository();
        string runId = Guid.NewGuid().ToString("N");
        string approvalId = "apr-par-" + Guid.NewGuid().ToString("N");
        GovernanceApprovalRequest item = NewApproval(approvalId, runId, DateTime.UtcNow);

        await repo.CreateAsync(item, CancellationToken.None);

        DateTime reviewedUtc = new(2026, 4, 10, 12, 0, 0, DateTimeKind.Utc);
        const int parallel = 32;
        Task<bool>[] tasks = new Task<bool>[parallel];

        for (int i = 0; i < parallel; i++)
        {
            int reviewerIndex = i;
            tasks[i] = repo.TryTransitionFromReviewableAsync(
                approvalId,
                GovernanceApprovalStatus.Approved,
                $"r{reviewerIndex}",
                $"r{reviewerIndex}",
                "parallel",
                reviewedUtc.AddTicks(reviewerIndex),
                CancellationToken.None);
        }

        bool[] outcomes = await Task.WhenAll(tasks);
        int wins = outcomes.Count(static b => b);

        wins.Should().Be(1);

        GovernanceApprovalRequest? loaded = await repo.GetByIdAsync(approvalId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.Status.Should().Be(GovernanceApprovalStatus.Approved);
    }

    [SkippableFact]
    public async Task GetByRunId_orders_descending_by_RequestedUtc()
    {
        SkipIfSqlServerUnavailable();
        IGovernanceApprovalRequestRepository repo = CreateRepository();
        string runId = Guid.NewGuid().ToString("N");
        string idOld = "apr-run-old-" + Guid.NewGuid().ToString("N");
        string idNew = "apr-run-new-" + Guid.NewGuid().ToString("N");
        // Separate ticks so ORDER BY RequestedUtc DESC stays deterministic vs shared-catalog copies that reuse the same calendar instants.
        DateTime newer = new(2026, 4, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        DateTime older = newer.AddMilliseconds(-500);

        await repo.CreateAsync(NewApproval(idOld, runId, older), CancellationToken.None);
        await repo.CreateAsync(NewApproval(idNew, runId, newer), CancellationToken.None);

        IReadOnlyList<GovernanceApprovalRequest> list = await repo.GetByRunIdAsync(runId, CancellationToken.None);

        list.Should().HaveCount(2);
        list[0].ApprovalRequestId.Should().Be(idNew);
        list[1].ApprovalRequestId.Should().Be(idOld);
    }

    [SkippableFact]
    public async Task GetPendingAsync_returns_draft_and_submitted_ordered_by_RequestedUtc_desc()
    {
        SkipIfSqlServerUnavailable();
        IGovernanceApprovalRequestRepository repo = CreateRepository();
        string runId = Guid.NewGuid().ToString("N");
        DateTime older = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
        DateTime newer = new(2026, 4, 1, 11, 0, 0, DateTimeKind.Utc);

        GovernanceApprovalRequest draftOld = NewApproval("apr-draft-old", runId, older);
        draftOld.Status = GovernanceApprovalStatus.Draft;
        await repo.CreateAsync(draftOld, CancellationToken.None);

        await repo.CreateAsync(NewApproval("apr-sub-new", runId, newer), CancellationToken.None);

        IReadOnlyList<GovernanceApprovalRequest> pending = await repo.GetPendingAsync(50, CancellationToken.None);

        // GetPendingAsync is scoped to the whole table (Draft/Submitted); SQL contract tests share one database.
        GovernanceApprovalRequest[] mine =
            [.. pending.Where(r => r.RunId == runId).OrderByDescending(r => r.RequestedUtc)];

        mine.Should().HaveCount(2);
        mine[0].ApprovalRequestId.Should().Be("apr-sub-new");
        mine[1].ApprovalRequestId.Should().Be("apr-draft-old");
    }

    [SkippableFact]
    public async Task GetPendingAsync_respects_maxRows()
    {
        SkipIfSqlServerUnavailable();
        IGovernanceApprovalRequestRepository repo = CreateRepository();
        string runId = Guid.NewGuid().ToString("N");
        string idA = "apr-max-a-" + Guid.NewGuid().ToString("N");
        string idB = "apr-max-b-" + Guid.NewGuid().ToString("N");
        string idC = "apr-max-c-" + Guid.NewGuid().ToString("N");
        // TOP (@MaxRows) is global; use end-of-range instants ahead of legacy 9999-01-* rows left in shared catalogs.
        DateTime t3 = new(9999, 12, 31, 23, 59, 59, 997, DateTimeKind.Utc);
        DateTime t2 = new(9999, 12, 31, 23, 59, 59, 996, DateTimeKind.Utc);
        DateTime t1 = new(9999, 12, 31, 23, 59, 59, 995, DateTimeKind.Utc);

        await repo.CreateAsync(NewApproval(idA, runId, t1), CancellationToken.None);
        await repo.CreateAsync(NewApproval(idB, runId, t2), CancellationToken.None);
        await repo.CreateAsync(NewApproval(idC, runId, t3), CancellationToken.None);

        IReadOnlyList<GovernanceApprovalRequest> pending = await repo.GetPendingAsync(2, CancellationToken.None);

        pending.Should().HaveCount(2);
        pending[0].ApprovalRequestId.Should().Be(idC);
        pending[1].ApprovalRequestId.Should().Be(idB);
    }

    [SkippableFact]
    public async Task GetRecentDecisionsAsync_orders_by_ReviewedUtc_desc_and_excludes_pending()
    {
        SkipIfSqlServerUnavailable();
        IGovernanceApprovalRequestRepository repo = CreateRepository();
        string runId = Guid.NewGuid().ToString("N");
        DateTime requested = new(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc);
        // TOP (@MaxRows) is global on the table under scope; far-future reviewed times keep this run inside the window
        // when the shared contract catalog already holds many decision rows from other tests.
        DateTime reviewedOlder = new(9999, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        DateTime reviewedNewer = new(9999, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        GovernanceApprovalRequest approved = NewApproval("apr-apr", runId, requested);
        approved.Status = GovernanceApprovalStatus.Approved;
        approved.ReviewedBy = "r1";
        approved.ReviewedUtc = reviewedOlder;
        await repo.CreateAsync(approved, CancellationToken.None);

        GovernanceApprovalRequest rejected = NewApproval("apr-rej", runId, requested);
        rejected.Status = GovernanceApprovalStatus.Rejected;
        rejected.ReviewedBy = "r2";
        rejected.ReviewedUtc = reviewedNewer;
        await repo.CreateAsync(rejected, CancellationToken.None);

        await repo.CreateAsync(NewApproval("apr-still-open", runId, requested), CancellationToken.None);

        IReadOnlyList<GovernanceApprovalRequest> decisions =
            await repo.GetRecentDecisionsAsync(50, CancellationToken.None);

        GovernanceApprovalRequest[] mine =
            [.. decisions.Where(r => r.RunId == runId).OrderByDescending(r => r.ReviewedUtc)];

        mine.Should().HaveCount(2);
        mine[0].ApprovalRequestId.Should().Be("apr-rej");
        mine[1].ApprovalRequestId.Should().Be("apr-apr");
    }

    private static GovernanceApprovalRequest NewApproval(string approvalId, string runId, DateTime requestedUtc)
    {
        return new GovernanceApprovalRequest
        {
            ApprovalRequestId = approvalId,
            RunId = runId,
            TenantId = GovernanceRepositoryContractScope.TenantId,
            WorkspaceId = GovernanceRepositoryContractScope.WorkspaceId,
            ProjectId = GovernanceRepositoryContractScope.ProjectId,
            ManifestVersion = "v1",
            SourceEnvironment = GovernanceEnvironment.Dev,
            TargetEnvironment = GovernanceEnvironment.Test,
            Status = GovernanceApprovalStatus.Submitted,
            RequestedBy = "alice",
            RequestedUtc = requestedUtc
        };
    }
}
