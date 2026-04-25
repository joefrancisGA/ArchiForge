using ArchLucid.Contracts.Governance;
using ArchLucid.Decisioning.Governance.PolicyPacks;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Shared contract assertions for <see cref="IPolicyPackChangeLogRepository" />.
/// </summary>
public abstract class PolicyPackChangeLogRepositoryContractTests
{
    private const int SeededChangeLogRowsForMaxRowsContractTest = 5;

    private static readonly Guid TenantA = Guid.Parse("a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1");
    private static readonly Guid WorkspaceW = Guid.Parse("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1");
    private static readonly Guid ProjectP = Guid.Parse("c1c1c1c1-c1c1-c1c1-c1c1-c1c1c1c1c1c1");
    private static readonly Guid TenantB = Guid.Parse("d2d2d2d2-d2d2-d2d2-d2d2-d2d2d2d2d2d2");
    protected abstract IPolicyPackChangeLogRepository CreateRepository();

    /// <summary>No-op for in-memory implementations; Dapper + SQL Server subclasses skip when no instance is available.</summary>
    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    private static PolicyPackChangeLogEntry CreateEntry(
        Guid policyPackId,
        Guid tenantId,
        string? summary = null,
        DateTime? changedUtc = null)
    {
        return new PolicyPackChangeLogEntry
        {
            PolicyPackId = policyPackId,
            TenantId = tenantId,
            WorkspaceId = WorkspaceW,
            ProjectId = ProjectP,
            ChangeType = PolicyPackChangeTypes.Created,
            ChangedBy = "tester",
            ChangedUtc = changedUtc ?? DateTime.UtcNow,
            PreviousValue = null,
            NewValue = null,
            SummaryText = summary
        };
    }

    [SkippableFact]
    public async Task AppendAsync_InsertsEntry_GetByPolicyPackIdReturnsIt()
    {
        SkipIfSqlServerUnavailable();
        IPolicyPackChangeLogRepository repo = CreateRepository();
        Guid packId = Guid.Parse("e1e1e1e1-e1e1-e1e1-e1e1-e1e1e1e1e1e1");

        await repo.AppendAsync(CreateEntry(packId, TenantA, "first"), CancellationToken.None);

        IReadOnlyList<PolicyPackChangeLogEntry> list =
            await repo.GetByPolicyPackIdAsync(packId, 50, CancellationToken.None);

        list.Should().ContainSingle(e => e.PolicyPackId == packId && e.SummaryText == "first");
    }

    [SkippableFact]
    public async Task GetByPolicyPackId_MultipleEntries_ReturnsDescendingByChangedUtc()
    {
        SkipIfSqlServerUnavailable();
        IPolicyPackChangeLogRepository repo = CreateRepository();
        Guid packId = Guid.Parse("f1f1f1f1-f1f1-f1f1-f1f1-f1f1f1f1f1f1");
        DateTime t0 = new(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);
        DateTime t1 = t0.AddHours(1);
        DateTime t2 = t0.AddHours(2);

        await repo.AppendAsync(CreateEntry(packId, TenantA, "old", t0), CancellationToken.None);
        await repo.AppendAsync(CreateEntry(packId, TenantA, "mid", t1), CancellationToken.None);
        await repo.AppendAsync(CreateEntry(packId, TenantA, "new", t2), CancellationToken.None);

        IReadOnlyList<PolicyPackChangeLogEntry> list =
            await repo.GetByPolicyPackIdAsync(packId, 50, CancellationToken.None);

        list.Should().HaveCount(3);
        list[0].SummaryText.Should().Be("new");
        list[1].SummaryText.Should().Be("mid");
        list[2].SummaryText.Should().Be("old");
    }

    [SkippableFact]
    public async Task GetByTenant_FiltersCorrectly()
    {
        SkipIfSqlServerUnavailable();
        IPolicyPackChangeLogRepository repo = CreateRepository();
        Guid packA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid packB = Guid.Parse("22222222-2222-2222-2222-222222222222");

        await repo.AppendAsync(CreateEntry(packA, TenantA, "tenant-a"), CancellationToken.None);
        await repo.AppendAsync(CreateEntry(packB, TenantB, "tenant-b"), CancellationToken.None);

        IReadOnlyList<PolicyPackChangeLogEntry> forA =
            await repo.GetByTenantAsync(TenantA, 100, CancellationToken.None);

        forA.Should().ContainSingle(e => e.SummaryText == "tenant-a");
        forA.Should().NotContain(e => e.SummaryText == "tenant-b");
    }

    [SkippableFact]
    public async Task GetByPolicyPackId_RespectsMaxRows()
    {
        SkipIfSqlServerUnavailable();
        IPolicyPackChangeLogRepository repo = CreateRepository();
        Guid packId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        DateTime t0 = new(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc);

        for (int i = 0; i < SeededChangeLogRowsForMaxRowsContractTest; i++)
        {
            await repo.AppendAsync(
                CreateEntry(packId, TenantA, $"row-{i}", t0.AddMinutes(i)),
                CancellationToken.None);
        }

        IReadOnlyList<PolicyPackChangeLogEntry> list =
            await repo.GetByPolicyPackIdAsync(packId, 2, CancellationToken.None);

        list.Should().HaveCount(2);
        list[0].SummaryText.Should().Be("row-4");
        list[1].SummaryText.Should().Be("row-3");
    }

    [SkippableFact]
    public async Task GetByTenantInRangeAsync_ReturnsAscending_ExcludesEnds()
    {
        SkipIfSqlServerUnavailable();
        IPolicyPackChangeLogRepository repo = CreateRepository();
        Guid packId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        DateTime from = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime to = new(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc);

        await repo.AppendAsync(
            CreateEntry(packId, TenantA, "before", from.AddHours(-1)),
            CancellationToken.None);
        await repo.AppendAsync(
            CreateEntry(packId, TenantA, "in1", from.AddHours(1)),
            CancellationToken.None);
        await repo.AppendAsync(
            CreateEntry(packId, TenantA, "in2", from.AddHours(2)),
            CancellationToken.None);
        await repo.AppendAsync(
            CreateEntry(packId, TenantA, "after", to),
            CancellationToken.None);

        IReadOnlyList<PolicyPackChangeLogEntry> list =
            await repo.GetByTenantInRangeAsync(TenantA, from, to, CancellationToken.None);

        list.Should().HaveCount(2);
        list.Select(e => e.SummaryText).Should().Equal("in1", "in2");
    }
}
