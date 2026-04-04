using ArchiForge.Contracts.Governance;
using ArchiForge.Persistence.Data.Repositories;

using FluentAssertions;

namespace ArchiForge.Persistence.Tests.Contracts;

/// <summary>
/// Shared contract assertions for <see cref="IGovernancePromotionRecordRepository"/>.
/// Ordering matches Dapper: <c>ORDER BY PromotedUtc DESC</c> (newest first).
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

        GovernancePromotionRecord item = NewPromotion(promotionId, runId, new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc));

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
        DateTime older = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
        DateTime newer = new(2026, 4, 1, 11, 0, 0, DateTimeKind.Utc);

        await repo.CreateAsync(NewPromotion("prm-old", runId, older), CancellationToken.None);
        await repo.CreateAsync(NewPromotion("prm-new", runId, newer), CancellationToken.None);

        IReadOnlyList<GovernancePromotionRecord> list = await repo.GetByRunIdAsync(runId, CancellationToken.None);

        list.Should().HaveCount(2);
        list[0].PromotionRecordId.Should().Be("prm-new");
        list[1].PromotionRecordId.Should().Be("prm-old");
    }

    private static GovernancePromotionRecord NewPromotion(string promotionId, string runId, DateTime promotedUtc)
    {
        return new GovernancePromotionRecord
        {
            PromotionRecordId = promotionId,
            RunId = runId,
            ManifestVersion = "v1",
            SourceEnvironment = GovernanceEnvironment.Dev,
            TargetEnvironment = GovernanceEnvironment.Test,
            PromotedBy = "bob",
            PromotedUtc = promotedUtc,
        };
    }
}
