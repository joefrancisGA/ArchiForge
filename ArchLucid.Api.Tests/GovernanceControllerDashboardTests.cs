using ArchLucid.Api.Controllers;
using ArchLucid.Application.Common;
using ArchLucid.Application.Governance;
using ArchLucid.Contracts.Governance;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Data.Repositories;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>
/// Unit coverage for governance dashboard HTTP wiring (scope + dashboard service).
/// </summary>
[Trait("Category", "Unit")]
public sealed class GovernanceControllerDashboardTests
{
    [Fact]
    public async Task GetDashboard_ReturnsOkWithSummary()
    {
        Guid tenantId = Guid.Parse("cccccccc-dddd-eeee-ffff-000011112222");

        Mock<IScopeContextProvider> scope = new();
        scope.Setup(s => s.GetCurrentScope()).Returns(new ScopeContext { TenantId = tenantId });

        GovernanceDashboardSummary expected = new()
        {
            PendingApprovals = [new GovernanceApprovalRequest { ApprovalRequestId = "x1" }],
            RecentDecisions = [],
            RecentChanges = [],
            PendingCount = 1,
        };

        Mock<IGovernanceDashboardService> dashboard = new();
        dashboard
            .Setup(
                d => d.GetDashboardAsync(
                    tenantId,
                    20,
                    20,
                    20,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        GovernanceController sut = new(
            Mock.Of<IGovernanceWorkflowService>(),
            Mock.Of<IGovernanceApprovalRequestRepository>(),
            Mock.Of<IGovernancePromotionRecordRepository>(),
            Mock.Of<IGovernanceEnvironmentActivationRepository>(),
            Mock.Of<IActorContext>(),
            scope.Object,
            dashboard.Object,
            Mock.Of<IGovernanceLineageService>(),
            Mock.Of<IGovernanceRationaleService>(),
            Mock.Of<IComplianceDriftTrendService>(),
            NullLogger<GovernanceController>.Instance);

        IActionResult result = await sut.GetDashboard(20, 20, 20, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        OkObjectResult ok = (OkObjectResult)result;
        ok.Value.Should().BeOfType<GovernanceDashboardSummary>();
        GovernanceDashboardSummary payload = (GovernanceDashboardSummary)ok.Value!;
        payload.PendingCount.Should().Be(1);
        payload.PendingApprovals.Should().ContainSingle().Which.ApprovalRequestId.Should().Be("x1");
    }
}
