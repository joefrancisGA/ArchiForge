using ArchLucid.Application.Governance;
using ArchLucid.Contracts.Governance;
using ArchLucid.Decisioning.Governance.PolicyPacks;
using ArchLucid.Persistence.Data.Repositories;

using FluentAssertions;

using Moq;

namespace ArchLucid.Application.Tests.Governance;

[Trait("Category", "Unit")]
public sealed class GovernanceDashboardServiceTests
{
    [SkippableFact]
    public async Task GetDashboard_ReturnsPendingAndDecisionsAndChanges()
    {
        Guid tenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        GovernanceApprovalRequest pending = new() { ApprovalRequestId = "p1", Status = GovernanceApprovalStatus.Submitted };
        GovernanceApprovalRequest decision = new() { ApprovalRequestId = "d1", Status = GovernanceApprovalStatus.Approved, ReviewedUtc = DateTime.UtcNow, };

        PolicyPackChangeLogEntry change = new()
        {
            ChangeLogId = Guid.NewGuid(),
            PolicyPackId = Guid.NewGuid(),
            TenantId = tenantId,
            ChangeType = "Published",
            ChangedBy = "u1",
            ChangedUtc = DateTime.UtcNow,
        };

        Mock<IGovernanceApprovalRequestRepository> approvals = new();
        approvals
            .Setup(a => a.GetPendingAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GovernanceApprovalRequest> { pending });

        approvals
            .Setup(a => a.GetRecentDecisionsAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GovernanceApprovalRequest> { decision });

        Mock<IPolicyPackChangeLogRepository> changes = new();
        changes
            .Setup(c => c.GetByTenantAsync(tenantId, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PolicyPackChangeLogEntry> { change });

        IGovernanceDashboardService sut = new GovernanceDashboardService(approvals.Object, changes.Object);

        GovernanceDashboardSummary summary = await sut.GetDashboardAsync(tenantId);

        summary.PendingApprovals.Should().ContainSingle().Which.ApprovalRequestId.Should().Be("p1");
        summary.RecentDecisions.Should().ContainSingle().Which.ApprovalRequestId.Should().Be("d1");
        summary.RecentChanges.Should().ContainSingle().Which.TenantId.Should().Be(tenantId);
        summary.PendingCount.Should().Be(1);
    }

    [SkippableFact]
    public async Task GetDashboard_EmptyState_ReturnsEmptyLists()
    {
        Guid tenantId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");

        Mock<IGovernanceApprovalRequestRepository> approvals = new();
        approvals.Setup(a => a.GetPendingAsync(20, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        approvals.Setup(a => a.GetRecentDecisionsAsync(20, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        Mock<IPolicyPackChangeLogRepository> changes = new();
        changes.Setup(c => c.GetByTenantAsync(tenantId, 20, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        IGovernanceDashboardService sut = new GovernanceDashboardService(approvals.Object, changes.Object);

        GovernanceDashboardSummary summary = await sut.GetDashboardAsync(tenantId);

        summary.PendingApprovals.Should().BeEmpty();
        summary.RecentDecisions.Should().BeEmpty();
        summary.RecentChanges.Should().BeEmpty();
        summary.PendingCount.Should().Be(0);
    }
}
