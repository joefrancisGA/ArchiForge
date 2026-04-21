using ArchLucid.Application;
using ArchLucid.Application.Pilots;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Application.Tests.Pilots;

[Trait("Suite", "Core")]
public sealed class FirstValueReportPdfBuilderTests
{
    [Fact]
    public async Task BuildPdfAsync_WhenRunMissing_ReturnsNull()
    {
        Mock<IRunDetailQueryService> query = new();
        query.Setup(q => q.GetRunDetailAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArchitectureRunDetail?)null);

        Mock<IPilotRunDeltaComputer> deltas = new();
        FirstValueReportBuilder markdown = new(query.Object, deltas.Object, NullLogger<FirstValueReportBuilder>.Instance);
        FirstValueReportPdfBuilder sut = new(markdown);

        byte[]? pdf = await sut.BuildPdfAsync("missing", "http://localhost:5000");

        pdf.Should().BeNull();
    }

    [Fact]
    public async Task BuildPdfAsync_WhenCommitted_ReturnsPdfWithMagicBytes()
    {
        ArchitectureRunDetail detail = BuildCommittedDetail();
        Mock<IRunDetailQueryService> query = new();
        query.Setup(q => q.GetRunDetailAsync("r-pdf-md-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);

        Mock<IPilotRunDeltaComputer> deltas = new();
        deltas.Setup(d => d.ComputeAsync(detail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PilotRunDeltas
            {
                RunCreatedUtc = detail.Run.CreatedUtc,
                ManifestCommittedUtc = detail.Manifest!.Metadata.CreatedUtc,
                TimeToCommittedManifest = detail.Manifest.Metadata.CreatedUtc - detail.Run.CreatedUtc,
                FindingsBySeverity = [],
                AuditRowCount = 0,
                LlmCallCount = 0,
            });

        FirstValueReportBuilder markdown = new(query.Object, deltas.Object, NullLogger<FirstValueReportBuilder>.Instance);
        FirstValueReportPdfBuilder sut = new(markdown);

        byte[]? pdf = await sut.BuildPdfAsync("r-pdf-md-1", "http://localhost:5000");

        pdf.Should().NotBeNull();
        pdf!.Length.Should().BeGreaterThan(64);
        ReadOnlySpan<byte> head = pdf.AsSpan(0, 4);
        head[0].Should().Be((byte)'%');
        head[1].Should().Be((byte)'P');
        head[2].Should().Be((byte)'D');
        head[3].Should().Be((byte)'F');
    }

    [Fact]
    public async Task BuildPdfAsync_NullRunId_Throws()
    {
        Mock<IRunDetailQueryService> query = new();
        Mock<IPilotRunDeltaComputer> deltas = new();
        FirstValueReportBuilder markdown = new(query.Object, deltas.Object, NullLogger<FirstValueReportBuilder>.Instance);
        FirstValueReportPdfBuilder sut = new(markdown);

        Func<Task> act = () => sut.BuildPdfAsync(string.Empty, "http://localhost:5000");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    private static ArchitectureRunDetail BuildCommittedDetail()
    {
        ArchitectureRun run = new()
        {
            RunId = "r-pdf-md-1",
            RequestId = "req",
            Status = ArchitectureRunStatus.Committed,
            CreatedUtc = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            CompletedUtc = new DateTime(2026, 4, 1, 1, 0, 0, DateTimeKind.Utc),
            CurrentManifestVersion = "v1",
        };

        GoldenManifest manifest = new()
        {
            RunId = "r-pdf-md-1",
            SystemName = "DemoSystem",
            Metadata = new ManifestMetadata { ManifestVersion = "v1", CreatedUtc = run.CreatedUtc },
            Governance = new ManifestGovernance(),
        };

        return new ArchitectureRunDetail
        {
            Run = run,
            Manifest = manifest,
            Results = [],
            DecisionTraces = [],
        };
    }
}
