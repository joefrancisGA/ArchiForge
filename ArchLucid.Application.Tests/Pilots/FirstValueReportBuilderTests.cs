using ArchLucid.Application.Pilots;
using ArchLucid.Application.Value;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Explanation;
using ArchLucid.Contracts.Findings;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Value;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Application.Tests.Pilots;

[Trait("Suite", "Core")]
public sealed class FirstValueReportBuilderTests
{
    [Fact]
    public async Task BuildMarkdownAsync_WhenRunMissing_ReturnsNull()
    {
        Mock<IRunDetailQueryService> query = new();
        query.Setup(q => q.GetRunDetailAsync("abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArchitectureRunDetail?)null);

        Mock<IPilotRunDeltaComputer> deltas = new();
        FirstValueReportBuilder sut = CreateSut(query.Object, deltas.Object);

        string? md = await sut.BuildMarkdownAsync("abc", "http://localhost:5000");

        md.Should().BeNull();
        deltas.Verify(d => d.ComputeAsync(It.IsAny<ArchitectureRunDetail>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BuildMarkdownAsync_WhenCommitted_RendersComputedDeltasAndManifest()
    {
        ArchitectureRunDetail detail = BuildCommittedDetail();
        Mock<IRunDetailQueryService> query = new();
        query.Setup(q => q.GetRunDetailAsync("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);

        PilotRunDeltas computed = new()
        {
            RunCreatedUtc = detail.Run.CreatedUtc,
            ManifestCommittedUtc = detail.Manifest!.Metadata.CreatedUtc,
            TimeToCommittedManifest = detail.Manifest.Metadata.CreatedUtc - detail.Run.CreatedUtc,
            FindingsBySeverity =
            [
                new KeyValuePair<string, int>("Warning", 2),
                new KeyValuePair<string, int>("Error", 1),
            ],
            AuditRowCount = 7,
            LlmCallCount = 4,
            TopFindingId = "top-finding-id",
            TopFindingSeverity = "Error",
            TopFindingEvidenceChain = new FindingEvidenceChainResponse
            {
                RunId = "r1",
                FindingId = "top-finding-id",
                ManifestVersion = "v2",
                FindingsSnapshotId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            },
            IsDemoTenant = false,
        };

        Mock<IPilotRunDeltaComputer> deltas = new();
        deltas.Setup(d => d.ComputeAsync(detail, It.IsAny<CancellationToken>())).ReturnsAsync(computed);

        FirstValueReportBuilder sut = CreateSut(query.Object, deltas.Object);

        string? md = await sut.BuildMarkdownAsync("r1", "http://api.test");

        md.Should().NotBeNull();
        md.Should().Contain("Computed deltas (from this run)");
        md.Should().Contain("Review-cycle delta (before vs measured)");
        md.Should().Contain("v2");
        md.Should().Contain("SysA");
        md.Should().Contain("| Warning | 2 |");
        md.Should().Contain("| Error | 1 |");
        md.Should().Contain("LLM calls for this run");
        md.Should().Contain("| Audit rows for this run | 7 |");
        md.Should().Contain("Top-severity finding — evidence chain excerpt");
        md.Should().Contain("`top-finding-id`");
        md.Should().Contain("11111111-1111-1111-1111-111111111111");
        md.Should().Contain("docs/EXECUTIVE_SPONSOR_BRIEF.md");
    }

    [Fact]
    public async Task BuildMarkdownAsync_WhenDemoTenant_RendersBannerAtTopAndOnDeltaSection()
    {
        ArchitectureRunDetail detail = BuildCommittedDetail();
        Mock<IRunDetailQueryService> query = new();
        query.Setup(q => q.GetRunDetailAsync("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);

        PilotRunDeltas computed = new()
        {
            RunCreatedUtc = detail.Run.CreatedUtc,
            ManifestCommittedUtc = detail.Manifest!.Metadata.CreatedUtc,
            TimeToCommittedManifest = detail.Manifest.Metadata.CreatedUtc - detail.Run.CreatedUtc,
            FindingsBySeverity = [],
            AuditRowCount = 0,
            LlmCallCount = 0,
            IsDemoTenant = true,
        };

        Mock<IPilotRunDeltaComputer> deltas = new();
        deltas.Setup(d => d.ComputeAsync(detail, It.IsAny<CancellationToken>())).ReturnsAsync(computed);

        FirstValueReportBuilder sut = CreateSut(query.Object, deltas.Object);

        string? md = await sut.BuildMarkdownAsync("r1", "http://api.test");

        md.Should().NotBeNull();
        // Banner must appear in the document preface AND immediately under the computed-deltas heading,
        // so a sponsor cannot crop the page and quote a single number out of context.
        int firstBanner = md.IndexOf("demo tenant — replace before publishing", StringComparison.Ordinal);
        int secondBanner = md.IndexOf("demo tenant — replace before publishing", firstBanner + 1, StringComparison.Ordinal);
        firstBanner.Should().BeGreaterThan(0);
        secondBanner.Should().BeGreaterThan(firstBanner);
    }

    private static ArchitectureRunDetail BuildCommittedDetail()
    {
        GoldenManifest manifest = new()
        {
            RunId = "r1",
            SystemName = "SysA",
            Metadata = new ManifestMetadata { ManifestVersion = "v2", CreatedUtc = new DateTime(2026, 4, 1, 0, 10, 0, DateTimeKind.Utc) },
            Governance = new ManifestGovernance(),
        };

        ArchitectureRun run = new()
        {
            RunId = "r1",
            RequestId = "req1",
            Status = ArchitectureRunStatus.Committed,
            CreatedUtc = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            CompletedUtc = new DateTime(2026, 4, 1, 0, 10, 0, DateTimeKind.Utc),
            CurrentManifestVersion = "v2",
        };

        AgentResult result = new()
        {
            TaskId = "t1",
            RunId = "r1",
            AgentType = AgentType.Topology,
            Findings =
            [
                new ArchitectureFinding { Severity = "Warning", Message = "m1" },
                new ArchitectureFinding { Severity = "warning", Message = "m2" },
                new ArchitectureFinding { Severity = "Error", Message = "m3" },
            ],
        };

        return new ArchitectureRunDetail
        {
            Run = run,
            Results = [result],
            Manifest = manifest,
            DecisionTraces = [],
        };
    }

    private static FirstValueReportBuilder CreateSut(IRunDetailQueryService query, IPilotRunDeltaComputer deltas)
    {
        Mock<IValueReportMetricsReader> metrics = new();
        metrics
            .Setup(m => m.ReadAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ValueReportRawMetrics(
                    [],
                    0,
                    0,
                    0,
                    0,
                    null,
                    null,
                    null,
                    null,
                    0));

        Mock<IOptionsMonitor<ValueReportComputationOptions>> opt = new();
        opt.Setup(o => o.CurrentValue).Returns(new ValueReportComputationOptions());

        ValueReportBuilder valueReport = new(metrics.Object, opt.Object);

        Mock<IScopeContextProvider> scope = new();
        scope.Setup(s => s.GetCurrentScope()).Returns(
            new ScopeContext
            {
                TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            });

        return new FirstValueReportBuilder(query, deltas, valueReport, scope.Object, NullLogger<FirstValueReportBuilder>.Instance);
    }
}
