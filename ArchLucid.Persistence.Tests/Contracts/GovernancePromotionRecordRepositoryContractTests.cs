using ArchLucid.Contracts.Governance;
using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Shared contract assertions for <see cref="IGovernancePromotionRecordRepository" />.
///     Ordering matches Dapper: <c>ORDER BY PromotedUtc DESC</c> (newest first).
/// </summary>
public abstract class GovernancePromotionRecordRepositoryContractTests
{
    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    protected abstract IGovernancePromotionRecordRepository CreateRepository();

    [SkippableFact]
    public async Task Create_then_GetByRunId_contains_record()
    {
        SkipIfSqlServerUnavailable();
        IGovernancePromotionRecordRepository repo = CreateRepository();
        string runId = Guid.NewGuid().ToString("N");
        string promotionId = "prm-" + Guid.NewGuid().ToString("N");
        // Mid-range UTC instants keep ORDER BY assertions distinct without hitting SqlClient's legacy SqlDateTime RPC edge cases near year 9999.
        DateTime promotedUtc = DistinctPromotionUtc(millisecondDelta: 0);

        GovernancePromotionRecord item = NewPromotion(promotionId, runId, promotedUtc);

        await repo.CreateAsync(item, CancellationToken.None);

        IReadOnlyList<GovernancePromotionRecord> list = await repo.GetByRunIdAsync(runId, CancellationToken.None);

        list.Should()
            .ContainSingle(r => string.Equals(r.PromotionRecordId, promotionId, StringComparison.Ordinal));
    }

    [SkippableFact]
    public async Task GetByRunId_orders_descending_by_PromotedUtc()
    {
        SkipIfSqlServerUnavailable();
        IGovernancePromotionRecordRepository repo = CreateRepository();
        string runId = Guid.NewGuid().ToString("N");
        string idOld = "prm-run-old-" + Guid.NewGuid().ToString("N");
        string idNew = "prm-run-new-" + Guid.NewGuid().ToString("N");
        // Distinct UTC instants; millisecond deltas remain distinct after DATETIME2 round-trip.
        DateTime newer = DistinctPromotionUtc(millisecondDelta: 1);
        DateTime older = DistinctPromotionUtc(millisecondDelta: 0);

        await repo.CreateAsync(NewPromotion(idOld, runId, older), CancellationToken.None);
        await repo.CreateAsync(NewPromotion(idNew, runId, newer), CancellationToken.None);

        IReadOnlyList<GovernancePromotionRecord> list = await repo.GetByRunIdAsync(runId, CancellationToken.None);

        GovernancePromotionRecord[] ours =
        [
            .. list.Where(r =>
                string.Equals(r.PromotionRecordId, idNew, StringComparison.Ordinal)
                || string.Equals(r.PromotionRecordId, idOld, StringComparison.Ordinal))
        ];

        ours.Should().HaveCount(2);
        ours[0].PromotionRecordId.Should().Be(idNew);
        ours[1].PromotionRecordId.Should().Be(idOld);
    }

    /// <summary>
    ///     Stable UTC timestamps for promotion ordering tests. Uses a fixed calendar date with whole-millisecond steps so pairs
    ///     stay strictly ordered after DATETIME2 round-trip while staying far from SqlClient SqlDateTime RPC boundary bugs.
    /// </summary>
    private static DateTime DistinctPromotionUtc(int millisecondDelta)
    {
        const int BaseMillisecond = 100;

        return new DateTime(2026, 6, 1, 12, 0, 0, BaseMillisecond + millisecondDelta, DateTimeKind.Utc);
    }

    private static GovernancePromotionRecord NewPromotion(string promotionId, string runId, DateTime promotedUtc)
    {
        return new GovernancePromotionRecord
        {
            PromotionRecordId = promotionId,
            RunId = runId,
            TenantId = GovernanceRepositoryContractScope.TenantId,
            WorkspaceId = GovernanceRepositoryContractScope.WorkspaceId,
            ProjectId = GovernanceRepositoryContractScope.ProjectId,
            ManifestVersion = "v1",
            SourceEnvironment = GovernanceEnvironment.Dev,
            TargetEnvironment = GovernanceEnvironment.Test,
            PromotedBy = "bob",
            PromotedUtc = promotedUtc
        };
    }
}
