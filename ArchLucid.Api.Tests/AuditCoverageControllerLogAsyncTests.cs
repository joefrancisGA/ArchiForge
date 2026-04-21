using System.Security.Claims;
using System.Text.Json;

using ArchLucid.AgentRuntime.Explanation;
using ArchLucid.Api.Controllers.Authority;
using ArchLucid.Application;
using ArchLucid.Application.Analysis;
using ArchLucid.ArtifactSynthesis.Docx;
using ArchLucid.ArtifactSynthesis.Docx.Models;
using ArchLucid.ArtifactSynthesis.Models;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Comparison;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Comparison;
using ArchLucid.Host.Core.Jobs;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Provenance;
using ArchLucid.Persistence.Queries;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using AppReplayExportRequest = ArchLucid.Application.Analysis.ReplayExportRequest;

namespace ArchLucid.Api.Tests;

/// <summary>
/// Verifies durable <see cref="IAuditService.LogAsync"/> wiring on controllers that previously lacked coverage.
/// </summary>
[Trait("Category", "Unit")]
public sealed class AnalysisReportsControllerAuditTests
{
    [Fact]
    public async Task AnalyzeRun_AfterSuccessfulBuild_LogsArchitectureAnalysisReportGeneratedWithDataJson()
    {
        string runId = Guid.NewGuid().ToString("N");
        ArchitectureRunDetail detail = new()
        {
            Run = new ArchitectureRun { RunId = runId },
        };

        ArchitectureAnalysisReport report = new()
        {
            Manifest = new GoldenManifest
            {
                Metadata = new ManifestMetadata { ManifestVersion = "v7" },
            },
            Warnings = ["a"],
        };

        Mock<IRunDetailQueryService> runDetailQuery = new();
        runDetailQuery
            .Setup(r => r.GetRunDetailAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);

        Mock<IArchitectureAnalysisService> analysis = new();
        analysis
            .Setup(a => a.BuildAsync(It.IsAny<ArchitectureAnalysisRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        Mock<IAuditService> audit = new();

        AnalysisReportsController sut = new(
            runDetailQuery.Object,
            analysis.Object,
            Mock.Of<IArchitectureAnalysisExportService>(),
            Mock.Of<IArchitectureAnalysisDocxExportService>(),
            Mock.Of<IArchitectureAnalysisConsultingDocxExportService>(),
            Mock.Of<IConsultingDocxTemplateRecommendationService>(),
            Mock.Of<IConsultingDocxExportProfileSelector>(),
            Mock.Of<IRunExportAuditService>(),
            Mock.Of<IBackgroundJobQueue>(),
            audit.Object,
            NullLogger<AnalysisReportsController>.Instance);

        sut.ControllerContext = CreateControllerContext();

        IActionResult response = await sut.AnalyzeRun(runId, new ArchitectureAnalysisRequest(), CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>();
        audit.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e =>
                    e.EventType == AuditEventTypes.ArchitectureAnalysisReportGenerated
                    && !string.IsNullOrWhiteSpace(e.DataJson)
                    && e.DataJson.Contains("\"manifestVersion\":\"v7\"", StringComparison.Ordinal)
                    && e.DataJson.Contains("\"warningCount\":1", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    internal static ControllerContext CreateControllerContext()
    {
        DefaultHttpContext http = new();
        http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "test-user")]));

        return new ControllerContext { HttpContext = http };
    }
}

[Trait("Category", "Unit")]
public sealed class DocxExportControllerAuditTests
{
    [Fact]
    public async Task ExportRunDocx_AfterSuccessfulExport_LogsArchitectureDocxExportGeneratedWithDataJson()
    {
        Guid runId = Guid.NewGuid();
        Guid manifestId = Guid.NewGuid();
        Guid? compareWith = Guid.NewGuid();

        ArchLucid.Decisioning.Models.GoldenManifest manifest = new()
        {
            ManifestId = manifestId,
            RuleSetId = "rs",
            RuleSetVersion = "1",
            RuleSetHash = "h",
            ManifestHash = "mh",
        };

        RunDetailDto runDetail = new()
        {
            Run = new RunRecord { RunId = runId },
            GoldenManifest = manifest,
        };

        Mock<IScopeContextProvider> scope = new();
        scope.Setup(s => s.GetCurrentScope()).Returns(new ScopeContext());

        Mock<IAuthorityQueryService> authority = new();
        authority
            .Setup(a => a.GetRunDetailAsync(It.IsAny<ScopeContext>(), runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(runDetail);
        authority
            .Setup(a => a.GetRunDetailAsync(It.IsAny<ScopeContext>(), compareWith.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new RunDetailDto
                {
                    Run = new RunRecord { RunId = compareWith.Value },
                    GoldenManifest = manifest,
                });

        Mock<IArtifactQueryService> artifacts = new();
        artifacts
            .Setup(a => a.GetArtifactsByManifestIdAsync(It.IsAny<ScopeContext>(), manifestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SynthesizedArtifact>());

        Mock<IComparisonService> comparison = new();
        comparison.Setup(c => c.Compare(
                It.IsAny<ArchLucid.Decisioning.Models.GoldenManifest>(),
                It.IsAny<ArchLucid.Decisioning.Models.GoldenManifest>()))
            .Returns(new ComparisonResult());

        byte[] payload = [1, 2, 3, 4];
        Mock<IDocxExportService> docx = new();
        docx
            .Setup(d => d.ExportAsync(It.IsAny<DocxExportRequest>(), manifest, It.IsAny<IReadOnlyList<SynthesizedArtifact>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new DocxExportResult
                {
                    Content = payload,
                    ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    FileName = "x.docx",
                });

        Mock<IAuditService> audit = new();

        DocxExportController sut = new(
            authority.Object,
            artifacts.Object,
            docx.Object,
            comparison.Object,
            Mock.Of<IExplanationService>(),
            Mock.Of<IProvenanceSnapshotRepository>(),
            scope.Object,
            audit.Object);

        sut.ControllerContext = AnalysisReportsControllerAuditTests.CreateControllerContext();

        IActionResult result = await sut.ExportRunDocx(runId, compareWith, explainRun: false, includeComparisonExplanation: false, CancellationToken.None);

        result.Should().BeOfType<FileContentResult>();
        audit.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e =>
                    e.EventType == AuditEventTypes.ArchitectureDocxExportGenerated
                    && e.RunId == runId
                    && e.ManifestId == manifestId
                    && !string.IsNullOrWhiteSpace(e.DataJson)),
                It.IsAny<CancellationToken>()),
            Times.Once);

        JsonDocument doc = JsonDocument.Parse(
            audit.Invocations[0].Arguments[0] is AuditEvent ev ? ev.DataJson : "{}");
        doc.RootElement.GetProperty("byteCount").GetInt32().Should().Be(payload.Length);
        doc.RootElement.GetProperty("compareWithRunId").GetGuid().Should().Be(compareWith.Value);
    }
}

[Trait("Category", "Unit")]
public sealed class ExportsControllerReplayExportAuditTests
{
    [Fact]
    public async Task ReplayExportRecord_WhenReplayPersisted_LogsReplayExportRecordedWithDataJson()
    {
        Mock<IExportReplayService> replay = new();
        replay
            .Setup(r => r.ReplayAsync(It.IsAny<AppReplayExportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ReplayExportResult
                {
                    ExportRecordId = "source-export",
                    RecordedReplayExportRecordId = "new-export-row",
                    RunId = "abc123def4567890abc123def4567890",
                    Format = "docx",
                    FileName = "r.docx",
                    Content = [],
                });

        Mock<IAuditService> audit = new();

        ExportsController sut = new(
            Mock.Of<IRunDetailQueryService>(),
            Mock.Of<IRunExportRecordRepository>(),
            Mock.Of<IComparisonAuditService>(),
            replay.Object,
            Mock.Of<IExportRecordDiffService>(),
            Mock.Of<IExportRecordDiffSummaryFormatter>(),
            audit.Object);

        DefaultHttpContext http = new();
        http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "u")]));
        http.Request.Method = "POST";
        sut.ControllerContext = new ControllerContext { HttpContext = http };

        await sut.ReplayExportRecord(
            "source-export",
            new ArchLucid.Api.Models.ReplayExportRequest { RecordReplayExport = true },
            CancellationToken.None);

        audit.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e =>
                    e.EventType == AuditEventTypes.ReplayExportRecorded
                    && !string.IsNullOrWhiteSpace(e.DataJson)
                    && e.DataJson.Contains("source-export", StringComparison.Ordinal)
                    && e.DataJson.Contains("new-export-row", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
