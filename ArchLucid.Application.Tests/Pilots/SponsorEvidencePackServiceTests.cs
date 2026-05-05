using ArchLucid.Application.Bootstrap;
using ArchLucid.Application.Governance;
using ArchLucid.Application.Pilots;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Governance;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Contracts.Pilots;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Models;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Application.Tests.Pilots;

[Trait("Suite", "Core")]
public sealed class SponsorEvidencePackServiceTests
{
    [SkippableFact]
    public async Task BuildAsync_when_demo_run_missing_exposes_null_deltas_and_zero_findings_completeness()
    {
        WhyArchLucidSnapshotResponse snap = new()
        {
            GeneratedUtc = new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            DemoRunId = ContosoRetailDemoIdentifiers.RunBaseline,
            RunsCreatedTotal = 3,
        };

        Mock<IWhyArchLucidSnapshotService> snapshot = new();
        snapshot.Setup(s => s.BuildAsync(It.IsAny<CancellationToken>())).ReturnsAsync(snap);

        Mock<IRunDetailQueryService> runs = new();
        runs.Setup(r => r.GetRunDetailAsync(ContosoRetailDemoIdentifiers.RunBaseline, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArchitectureRunDetail?)null);

        Mock<IPilotRunDeltaComputer> deltas = new();
        Mock<IFindingsSnapshotRepository> findingsRepo = new();

        GovernanceDashboardSummary dash = new() { PendingCount = 0, RecentDecisions = [], RecentChanges = [], };

        Mock<IGovernanceDashboardService> gov = new();
        gov.Setup(g =>
                g.GetDashboardAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(dash);

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(sp => sp.GetCurrentScope()).Returns(
            new ScopeContext { TenantId = ScopeIds.DefaultTenant, WorkspaceId = ScopeIds.DefaultWorkspace, ProjectId = ScopeIds.DefaultProject, });

        SponsorEvidencePackService sut = new(
            snapshot.Object,
            runs.Object,
            deltas.Object,
            findingsRepo.Object,
            gov.Object,
            scopeProvider.Object,
            NullLogger<SponsorEvidencePackService>.Instance);

        SponsorEvidencePackResponse result = await sut.BuildAsync(CancellationToken.None);

        result.DemoRunValueReportDelta.Should().BeNull();
        result.ExplainabilityTrace.TotalFindings.Should().Be(0);
        deltas.Verify(
            d => d.ComputeAsync(It.IsAny<ArchitectureRunDetail>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
    public async Task BuildAsync_loads_findings_snapshot_and_computes_pilot_delta_when_run_present()
    {
        Guid snapshotId = Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

        ArchitectureRunDetail detail = new() { Run = new ArchitectureRun { RunId = "runid", RequestId = "req", FindingsSnapshotId = snapshotId, }, };

        FindingsSnapshot persisted = new()
        {
            FindingsSnapshotId = snapshotId,
            Findings =
            [
                new Finding
                {
                    FindingId = "f1",
                    EngineType = "TestEngine",
                    FindingType = "type",
                    Category = "cat",
                    Severity = FindingSeverity.Warning,
                    Title = "t",
                    Rationale = "r",
                    Trace = new(),
                },
            ],
        };

        WhyArchLucidSnapshotResponse snap = new() { DemoRunId = "runid", GeneratedUtc = DateTimeOffset.UtcNow, };

        Mock<IWhyArchLucidSnapshotService> snapshot = new();
        snapshot.Setup(s => s.BuildAsync(It.IsAny<CancellationToken>())).ReturnsAsync(snap);

        Mock<IRunDetailQueryService> runs = new();
        runs.Setup(r => r.GetRunDetailAsync("runid", It.IsAny<CancellationToken>())).ReturnsAsync(detail);

        Mock<IPilotRunDeltaComputer> deltas = new();
        deltas.Setup(d => d.ComputeAsync(detail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new PilotRunDeltas
                {
                    RunCreatedUtc = DateTime.UtcNow, AuditRowCount = 1, LlmCallCount = 2, IsDemoTenant = true,
                });

        Mock<IFindingsSnapshotRepository> findingsRepo = new();
        findingsRepo
            .Setup(f => f.GetByIdAsync(snapshotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(persisted);

        Mock<IGovernanceDashboardService> gov = new();
        gov.Setup(g =>
                g.GetDashboardAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new GovernanceDashboardSummary { PendingCount = 2, RecentDecisions = [new GovernanceApprovalRequest()], RecentChanges = [], });

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(sp => sp.GetCurrentScope()).Returns(
            new ScopeContext { TenantId = ScopeIds.DefaultTenant, WorkspaceId = ScopeIds.DefaultWorkspace, ProjectId = ScopeIds.DefaultProject, });

        SponsorEvidencePackService sut = new(
            snapshot.Object,
            runs.Object,
            deltas.Object,
            findingsRepo.Object,
            gov.Object,
            scopeProvider.Object,
            NullLogger<SponsorEvidencePackService>.Instance);

        SponsorEvidencePackResponse result = await sut.BuildAsync(CancellationToken.None);

        result.DemoRunValueReportDelta.Should().NotBeNull();
        result.DemoRunValueReportDelta!.IsDemoTenant.Should().BeTrue();
        result.ExplainabilityTrace.TotalFindings.Should().Be(1);
        result.GovernanceOutcomes.PendingApprovalCount.Should().Be(2);
    }
}
