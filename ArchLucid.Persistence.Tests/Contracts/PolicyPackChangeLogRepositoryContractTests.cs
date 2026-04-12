using ArchLucid.Contracts.Governance;
using ArchLucid.Decisioning.Governance.PolicyPacks;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
/// Shared contract assertions for <see cref="IPolicyPackChangeLogRepository"/>.
/// </summary>
public abstract class PolicyPackChangeLogRepositoryContractTests
{
    protected abstract IPolicyPackChangeLogRepository CreateRepository();

    /// <summary>No-op for in-memory implementations; Dapper + SQL Server subclasses skip when no instance is available.</summary>
    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    private static readonly Guid TenantA = Guid.Parse("a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1");
    private static readonly Guid WorkspaceW = Guid.Parse("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1");
    private static readonly Guid ProjectP = Guid.Parse("c1c1c1c1-c1c1-c1c1-c1c1-c1c1c1c1c1c1");
    private static readonly Guid TenantB = Guid.Parse("d2d2d2d2-d2d2-d2d2-d2d2-d2d2d2d2d2d2");

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
            SummaryText = summary,
        };
    }

    [SkippableFact]
    public async Task AppendAsync_InsertsEntry_GetByPolicyPackIdReturnsIt()
    {
        SkipIfSqlServerUnavailable();
        IPolicyPackChangeLogRepository repo = CreateRepository();
        Guid packId = Guid.Parse("e1e1e1e1-e1e1-e1e1-e1e1-e1e1e1e1e1e1");

        await repo.AppendAsync(CreateEntry(packId, TenantA, summary: "first"), CancellationToken.None);

        IReadOnlyList<PolicyPackChangeLogEntry> list =
            await repo.GetByPolicyPackIdAsync(packId, maxRows: 50, CancellationToken.None);

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

        await repo.AppendAsync(CreateEntry(packId, TenantA, summary: "old", changedUtc: t0), CancellationToken.None);
        await repo.AppendAsync(CreateEntry(packId, TenantA, summary: "mid", changedUtc: t1), CancellationToken.None);
        await repo.AppendAsync(CreateEntry(packId, TenantA, summary: "new", changedUtc: t2), CancellationToken.None);

        IReadOnlyList<PolicyPackChangeLogEntry> list =
            await repo.GetByPolicyPackIdAsync(packId, maxRows: 50, CancellationToken.None);

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

        await repo.AppendAsync(CreateEntry(packA, TenantA, summary: "tenant-a"), CancellationToken.None);
        await repo.AppendAsync(CreateEntry(packB, TenantB, summary: "tenant-b"), CancellationToken.None);

        IReadOnlyList<PolicyPackChangeLogEntry> forA =
            await repo.GetByTenantAsync(TenantA, maxRows: 100, CancellationToken.None);

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

        for (int i = 0; i < 5; i++)
        {
            await repo.AppendAsync(
                CreateEntry(packId, TenantA, summary: $"row-{i}", changedUtc: t0.AddMinutes(i)),
                CancellationToken.None);
        }

        IReadOnlyList<PolicyPackChangeLogEntry> list =
            await repo.GetByPolicyPackIdAsync(packId, maxRows: 2, CancellationToken.None);

        list.Should().HaveCount(2);
        list[0].SummaryText.Should().Be("row-4");
        list[1].SummaryText.Should().Be("row-3");
    }
}
