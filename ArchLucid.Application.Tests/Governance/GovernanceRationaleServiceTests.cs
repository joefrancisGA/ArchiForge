using ArchLucid.Application.Governance;
using ArchLucid.Contracts.Governance;

using FluentAssertions;

using Moq;

namespace ArchLucid.Application.Tests.Governance;

[Trait("Category", "Unit")]
public sealed class GovernanceRationaleServiceTests
{
    [SkippableFact]
    public async Task GetApprovalRequestRationaleAsync_returns_null_when_lineage_missing()
    {
        Mock<IGovernanceLineageService> lineage = new();
        lineage
            .Setup(l => l.GetApprovalRequestLineageAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GovernanceLineageResult?)null);

        GovernanceRationaleService sut = new(lineage.Object);

        GovernanceRationaleResult? r = await sut.GetApprovalRequestRationaleAsync("missing");

        r.Should().BeNull();
    }

    [SkippableFact]
    public async Task GetApprovalRequestRationaleAsync_builds_summary_and_bullets()
    {
        Mock<IGovernanceLineageService> lineage = new();
        lineage
            .Setup(l => l.GetApprovalRequestLineageAsync("apr-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new GovernanceLineageResult
                {
                    ApprovalRequest = new GovernanceApprovalRequest
                    {
                        ApprovalRequestId = "apr-1",
                        RunId = "run-a",
                        ManifestVersion = "v2",
                        SourceEnvironment = "dev",
                        TargetEnvironment = "test",
                        Status = GovernanceApprovalStatus.Submitted,
                    },
                    Run = new GovernanceLineageRunSummary { RunId = "run-a", Status = "Ready", CreatedUtc = DateTime.UtcNow },
                    Manifest = new GovernanceLineageManifestSummary
                    {
                        ManifestVersion = "v2", DecisionCount = 3, UnresolvedIssueCount = 1, ComplianceGapCount = 0,
                    },
                    RiskPosture = "Medium",
                    TopFindings = [],
                    Promotions = [],
                });

        GovernanceRationaleService sut = new(lineage.Object);

        GovernanceRationaleResult? r = await sut.GetApprovalRequestRationaleAsync("apr-1");

        r.Should().NotBeNull();
        r.ApprovalRequestId.Should().Be("apr-1");
        r.Summary.Should().Contain("apr-1");
        r.Bullets.Should().Contain(b => b.Contains("dev", StringComparison.Ordinal) && b.Contains("test", StringComparison.Ordinal));
        r.Bullets.Should().Contain(b => b.Contains("Medium", StringComparison.Ordinal));
    }
}
