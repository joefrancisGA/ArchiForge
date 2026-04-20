using ArchLucid.Application;
using ArchLucid.Application.Pilots;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Findings;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

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

        FirstValueReportBuilder sut = new(query.Object, NullLogger<FirstValueReportBuilder>.Instance);

        string? md = await sut.BuildMarkdownAsync("abc", "http://localhost:5000");

        md.Should().BeNull();
    }

    [Fact]
    public async Task BuildMarkdownAsync_WhenCommitted_IncludesManifestVersionAndFindingCounts()
    {
        GoldenManifest manifest = new()
        {
            RunId = "r1",
            SystemName = "SysA",
            Metadata = new ManifestMetadata { ManifestVersion = "v2", CreatedUtc = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc) },
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
                new ArchitectureFinding { Severity = "Error", Message = "m3" }
            ]
        };

        ArchitectureRunDetail detail = new()
        {
            Run = run,
            Results = [result],
            Manifest = manifest,
            DecisionTraces = [],
        };

        Mock<IRunDetailQueryService> query = new();
        query.Setup(q => q.GetRunDetailAsync("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);

        FirstValueReportBuilder sut = new(query.Object, NullLogger<FirstValueReportBuilder>.Instance);

        string? md = await sut.BuildMarkdownAsync("r1", "http://api.test");

        md.Should().NotBeNull();
        md!.Should().Contain("v2");
        md.Should().Contain("SysA");
        md.Should().Contain("Warning");
        md.Should().Contain("| Error | 1 |");
        md.Should().Contain("docs/EXECUTIVE_SPONSOR_BRIEF.md");
    }
}
