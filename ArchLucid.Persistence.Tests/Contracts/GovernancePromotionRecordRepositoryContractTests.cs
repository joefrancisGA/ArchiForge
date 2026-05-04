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

        GovernancePromotionRecord item = NewPromotion(promotionId, runId,
            new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc));

        await repo.CreateAsync(item, CancellationToken.None);

        IReadOnlyList<GovernancePromotionRecord> list = await repo.GetByRunIdAsync(runId, CancellationToken.None);

        list.Should().ContainSingle();
        list[0].PromotionRecordId.Should().Be(promotionId);
    }

    [SkippableFact]
    public async Task GetByRunId_orders_descending_by_PromotedUtc()
    {
        SkipIfSqlServerUnavailable();
        IGovernancePromotionRecordRepository repo = CreateRepository();
        string runId = Guid.NewGuid().ToString("N");
        string idOld = "prm-run-old-" + Guid.NewGuid().ToString("N");
        string idNew = "prm-run-new-" + Guid.NewGuid().ToString("N");
        // Distinct sub-ms instants at the DATETIME2 ceiling — stable ORDER BY vs ties or dirty shared catalogs.
        DateTime newer = DateTime.MaxValue.AddTicks(-2);
        DateTime older = DateTime.MaxValue.AddTicks(-4);

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
