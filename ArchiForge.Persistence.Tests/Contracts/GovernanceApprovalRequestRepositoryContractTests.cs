using ArchiForge.Contracts.Governance;
using ArchiForge.Persistence.Data.Repositories;

using FluentAssertions;

namespace ArchiForge.Persistence.Tests.Contracts;

/// <summary>
/// Shared contract assertions for <see cref="IGovernanceApprovalRequestRepository"/>.
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
    public async Task GetByRunId_orders_descending_by_RequestedUtc()
    {
        SkipIfSqlServerUnavailable();
        IGovernanceApprovalRequestRepository repo = CreateRepository();
        string runId = Guid.NewGuid().ToString("N");
        DateTime older = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
        DateTime newer = new(2026, 4, 1, 11, 0, 0, DateTimeKind.Utc);

        await repo.CreateAsync(NewApproval("apr-old", runId, older), CancellationToken.None);
        await repo.CreateAsync(NewApproval("apr-new", runId, newer), CancellationToken.None);

        IReadOnlyList<GovernanceApprovalRequest> list = await repo.GetByRunIdAsync(runId, CancellationToken.None);

        list.Should().HaveCount(2);
        list[0].ApprovalRequestId.Should().Be("apr-new");
        list[1].ApprovalRequestId.Should().Be("apr-old");
    }

    private static GovernanceApprovalRequest NewApproval(string approvalId, string runId, DateTime requestedUtc)
    {
        return new GovernanceApprovalRequest
        {
            ApprovalRequestId = approvalId,
            RunId = runId,
            ManifestVersion = "v1",
            SourceEnvironment = GovernanceEnvironment.Dev,
            TargetEnvironment = GovernanceEnvironment.Test,
            Status = GovernanceApprovalStatus.Submitted,
            RequestedBy = "alice",
            RequestedUtc = requestedUtc,
        };
    }
}
