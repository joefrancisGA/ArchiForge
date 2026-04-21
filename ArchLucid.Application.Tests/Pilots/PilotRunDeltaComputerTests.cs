using ArchLucid.Application.Bootstrap;
using ArchLucid.Application.Explanation;
using ArchLucid.Application.Pilots;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Explanation;
using ArchLucid.Contracts.Findings;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Audit;
using ArchLucid.Persistence.Data.Repositories;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Application.Tests.Pilots;

[Trait("Suite", "Core")]
public sealed class PilotRunDeltaComputerTests
{
    [Fact]
    public async Task ComputeAsync_WhenCommittedAndScoped_PopulatesEveryDelta()
    {
        Guid runGuid = Guid.Parse("aaaaaaaa-1111-2222-3333-444444444444");
        ArchitectureRunDetail detail = BuildDetail(runGuid, isDemoSeed: false);

        Mock<IFindingEvidenceChainService> evidence = new();
        Mock<IAgentExecutionTraceRepository> traces = new();
        Mock<IAuditRepository> audit = new();
        Mock<IScopeContextProvider> scope = new();

        ScopeContext sc = new() { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() };
        scope.Setup(s => s.GetCurrentScope()).Returns(sc);

        traces.Setup(t => t.GetByRunIdAsync(detail.Run.RunId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new AgentExecutionTrace { TraceId = "t-1" },
                new AgentExecutionTrace { TraceId = "t-2" },
                new AgentExecutionTrace { TraceId = "t-3" },
            ]);

        audit.Setup(a => a.GetFilteredAsync(
                sc.TenantId,
                sc.WorkspaceId,
                sc.ProjectId,
                It.Is<AuditEventFilter>(f => f.RunId == runGuid),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new AuditEvent { EventType = "RunCreated", ActorUserId = "u", ActorUserName = "u" },
                new AuditEvent { EventType = "RunCommitted", ActorUserId = "u", ActorUserName = "u" },
            ]);

        FindingEvidenceChainResponse chain = new()
        {
            RunId = detail.Run.RunId,
            FindingId = "top-finding",
            ManifestVersion = "v9",
            FindingsSnapshotId = Guid.NewGuid(),
            DecisionTraceId = Guid.NewGuid(),
        };

        evidence.Setup(e => e.BuildAsync(detail.Run.RunId, "top-finding", It.IsAny<CancellationToken>()))
            .ReturnsAsync(chain);

        PilotRunDeltaComputer sut = new(
            evidence.Object,
            traces.Object,
            audit.Object,
            scope.Object,
            NullLogger<PilotRunDeltaComputer>.Instance);

        PilotRunDeltas result = await sut.ComputeAsync(detail);

        result.RunCreatedUtc.Should().Be(detail.Run.CreatedUtc);
        result.ManifestCommittedUtc.Should().Be(detail.Manifest!.Metadata.CreatedUtc);
        result.TimeToCommittedManifest.Should().Be(TimeSpan.FromMinutes(15));
        result.LlmCallCount.Should().Be(3);
        result.AuditRowCount.Should().Be(2);
        result.AuditRowCountTruncated.Should().BeFalse();
        result.FindingsBySeverity.Should().ContainInOrder(
            new KeyValuePair<string, int>("Warning", 2),
            new KeyValuePair<string, int>("Error", 1));
        result.TopFindingId.Should().Be("top-finding");
        result.TopFindingSeverity.Should().Be("Error");
        result.TopFindingEvidenceChain.Should().Be(chain);
        result.IsDemoTenant.Should().BeFalse();
    }

    [Fact]
    public async Task ComputeAsync_WhenRunIsCanonicalDemoBaseline_FlagsIsDemoTenant()
    {
        Guid runGuid = ContosoRetailDemoIdentifiers.AuthorityRunBaselineId;
        ArchitectureRunDetail detail = BuildDetail(runGuid, isDemoSeed: false);
        detail.Run.RunId = ContosoRetailDemoIdentifiers.RunBaseline;

        PilotRunDeltaComputer sut = BuildSutWithEmptyDependencies(out _, out _, out _, out _);

        PilotRunDeltas result = await sut.ComputeAsync(detail);

        result.IsDemoTenant.Should().BeTrue();
    }

    [Fact]
    public async Task ComputeAsync_WhenRequestIdMatchesMultiTenantDemoPrefix_FlagsIsDemoTenant()
    {
        Guid runGuid = Guid.NewGuid();
        ArchitectureRunDetail detail = BuildDetail(runGuid, isDemoSeed: false);
        detail.Run.RequestId = "req-contoso-demo-abc123def456";

        PilotRunDeltaComputer sut = BuildSutWithEmptyDependencies(out _, out _, out _, out _);

        PilotRunDeltas result = await sut.ComputeAsync(detail);

        result.IsDemoTenant.Should().BeTrue();
    }

    [Fact]
    public async Task ComputeAsync_WhenAuditRowsHitCap_MarksTruncated()
    {
        Guid runGuid = Guid.Parse("bbbbbbbb-1111-2222-3333-444444444444");
        ArchitectureRunDetail detail = BuildDetail(runGuid, isDemoSeed: false);

        Mock<IFindingEvidenceChainService> evidence = new();
        Mock<IAgentExecutionTraceRepository> traces = new();
        Mock<IAuditRepository> audit = new();
        Mock<IScopeContextProvider> scope = new();
        scope.Setup(s => s.GetCurrentScope()).Returns(new ScopeContext { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() });
        traces.Setup(t => t.GetByRunIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        // Return exactly the cap (500) so the computer flags the count as a lower bound.
        AuditEvent[] events = Enumerable.Range(0, 500)
            .Select(_ => new AuditEvent { EventType = "Noise", ActorUserId = "u", ActorUserName = "u" })
            .ToArray();
        audit.Setup(a => a.GetFilteredAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<AuditEventFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        PilotRunDeltaComputer sut = new(
            evidence.Object, traces.Object, audit.Object, scope.Object,
            NullLogger<PilotRunDeltaComputer>.Instance);

        PilotRunDeltas result = await sut.ComputeAsync(detail);

        result.AuditRowCount.Should().Be(500);
        result.AuditRowCountTruncated.Should().BeTrue();
    }

    [Fact]
    public async Task ComputeAsync_WhenAuditRepositoryThrows_ReportsZeroAndContinues()
    {
        Guid runGuid = Guid.Parse("cccccccc-1111-2222-3333-444444444444");
        ArchitectureRunDetail detail = BuildDetail(runGuid, isDemoSeed: false);

        Mock<IFindingEvidenceChainService> evidence = new();
        Mock<IAgentExecutionTraceRepository> traces = new();
        Mock<IAuditRepository> audit = new();
        Mock<IScopeContextProvider> scope = new();
        scope.Setup(s => s.GetCurrentScope()).Returns(new ScopeContext { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() });
        traces.Setup(t => t.GetByRunIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        audit.Setup(a => a.GetFilteredAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<AuditEventFilter>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("audit store offline"));

        PilotRunDeltaComputer sut = new(
            evidence.Object, traces.Object, audit.Object, scope.Object,
            NullLogger<PilotRunDeltaComputer>.Instance);

        PilotRunDeltas result = await sut.ComputeAsync(detail);

        result.AuditRowCount.Should().Be(0);
        result.AuditRowCountTruncated.Should().BeFalse();
    }

    [Fact]
    public async Task ComputeAsync_WhenNoFindings_LeavesEvidenceFieldsNull()
    {
        Guid runGuid = Guid.Parse("dddddddd-1111-2222-3333-444444444444");
        ArchitectureRunDetail detail = BuildDetail(runGuid, isDemoSeed: false);
        detail.Results = []; // No findings on this run.

        PilotRunDeltaComputer sut = BuildSutWithEmptyDependencies(
            out _,
            out Mock<IAgentExecutionTraceRepository> traces,
            out _,
            out Mock<IFindingEvidenceChainService> evidence);

        PilotRunDeltas result = await sut.ComputeAsync(detail);

        result.FindingsBySeverity.Should().BeEmpty();
        result.TopFindingId.Should().BeNull();
        result.TopFindingSeverity.Should().BeNull();
        result.TopFindingEvidenceChain.Should().BeNull();
        evidence.Verify(e => e.BuildAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        traces.Verify(t => t.GetByRunIdAsync(detail.Run.RunId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ComputeAsync_WhenManifestMissing_TimeToCommitIsNull()
    {
        Guid runGuid = Guid.Parse("eeeeeeee-1111-2222-3333-444444444444");
        ArchitectureRunDetail detail = BuildDetail(runGuid, isDemoSeed: false);
        detail.Manifest = null;

        PilotRunDeltaComputer sut = BuildSutWithEmptyDependencies(out _, out _, out _, out _);

        PilotRunDeltas result = await sut.ComputeAsync(detail);

        result.TimeToCommittedManifest.Should().BeNull();
        result.ManifestCommittedUtc.Should().BeNull();
    }

    [Fact]
    public async Task ComputeAsync_NullDetail_Throws()
    {
        PilotRunDeltaComputer sut = BuildSutWithEmptyDependencies(out _, out _, out _, out _);

        Func<Task> act = () => sut.ComputeAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ComputeAsync_WhenEvidenceChainThrows_ReportsNullChainAndKeepsTopFindingId()
    {
        Guid runGuid = Guid.Parse("ffffffff-1111-2222-3333-444444444444");
        ArchitectureRunDetail detail = BuildDetail(runGuid, isDemoSeed: false);

        Mock<IFindingEvidenceChainService> evidence = new();
        evidence.Setup(e => e.BuildAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("chain unavailable"));

        Mock<IAgentExecutionTraceRepository> traces = new();
        traces.Setup(t => t.GetByRunIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        Mock<IAuditRepository> audit = new();
        audit.Setup(a => a.GetFilteredAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<AuditEventFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        Mock<IScopeContextProvider> scope = new();
        scope.Setup(s => s.GetCurrentScope()).Returns(new ScopeContext { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() });

        PilotRunDeltaComputer sut = new(evidence.Object, traces.Object, audit.Object, scope.Object, NullLogger<PilotRunDeltaComputer>.Instance);

        PilotRunDeltas result = await sut.ComputeAsync(detail);

        result.TopFindingId.Should().NotBeNull();
        result.TopFindingEvidenceChain.Should().BeNull();
    }

    [Fact]
    public async Task ComputeAsync_WhenRunIdNotGuid_ReportsZeroAuditRowsWithoutQuerying()
    {
        ArchitectureRunDetail detail = BuildDetail(Guid.NewGuid(), isDemoSeed: false);
        detail.Run.RunId = "not-a-guid"; // Cannot map to dbo.AuditEvents.RunId.

        Mock<IFindingEvidenceChainService> evidence = new();
        Mock<IAgentExecutionTraceRepository> traces = new();
        traces.Setup(t => t.GetByRunIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        Mock<IAuditRepository> audit = new();
        Mock<IScopeContextProvider> scope = new();
        scope.Setup(s => s.GetCurrentScope()).Returns(new ScopeContext { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() });

        PilotRunDeltaComputer sut = new(evidence.Object, traces.Object, audit.Object, scope.Object, NullLogger<PilotRunDeltaComputer>.Instance);

        PilotRunDeltas result = await sut.ComputeAsync(detail);

        result.AuditRowCount.Should().Be(0);
        audit.Verify(a => a.GetFilteredAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<AuditEventFilter>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static PilotRunDeltaComputer BuildSutWithEmptyDependencies(
        out Mock<IAuditRepository> audit,
        out Mock<IAgentExecutionTraceRepository> traces,
        out Mock<IScopeContextProvider> scope,
        out Mock<IFindingEvidenceChainService> evidence)
    {
        evidence = new Mock<IFindingEvidenceChainService>();
        traces = new Mock<IAgentExecutionTraceRepository>();
        traces.Setup(t => t.GetByRunIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        audit = new Mock<IAuditRepository>();
        audit.Setup(a => a.GetFilteredAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<AuditEventFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        scope = new Mock<IScopeContextProvider>();
        scope.Setup(s => s.GetCurrentScope()).Returns(new ScopeContext { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() });
        evidence.Setup(e => e.BuildAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FindingEvidenceChainResponse?)null);

        return new PilotRunDeltaComputer(evidence.Object, traces.Object, audit.Object, scope.Object, NullLogger<PilotRunDeltaComputer>.Instance);
    }

    private static ArchitectureRunDetail BuildDetail(Guid runGuid, bool isDemoSeed)
    {
        DateTime created = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        ArchitectureRun run = new()
        {
            RunId = runGuid.ToString("N"),
            RequestId = isDemoSeed ? ContosoRetailDemoIdentifiers.RequestContoso : "req-1",
            Status = ArchitectureRunStatus.Committed,
            CreatedUtc = created,
            CompletedUtc = created.AddMinutes(15),
            CurrentManifestVersion = "v9",
        };

        GoldenManifest manifest = new()
        {
            RunId = run.RunId,
            SystemName = "Demo",
            Metadata = new ManifestMetadata { ManifestVersion = "v9", CreatedUtc = created.AddMinutes(15) },
            Governance = new ManifestGovernance(),
        };

        AgentResult result = new()
        {
            TaskId = "t1",
            RunId = run.RunId,
            AgentType = AgentType.Topology,
            Findings =
            [
                new ArchitectureFinding { FindingId = "warn-1", Severity = "Warning", Message = "m1" },
                new ArchitectureFinding { FindingId = "warn-2", Severity = "warning", Message = "m2" },
                new ArchitectureFinding { FindingId = "top-finding", Severity = "Error", Message = "m3" },
            ],
        };

        return new ArchitectureRunDetail
        {
            Run = run,
            Manifest = manifest,
            Results = [result],
            DecisionTraces = [],
        };
    }
}
