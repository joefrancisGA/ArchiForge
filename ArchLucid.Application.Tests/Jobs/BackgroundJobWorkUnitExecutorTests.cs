using ArchLucid.Application.Analysis;
using ArchLucid.Application.Jobs;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Core.Audit;

using FluentAssertions;

using Moq;

namespace ArchLucid.Application.Tests.Jobs;

[Trait("Category", "Unit")]
public sealed class BackgroundJobWorkUnitExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_AnalysisDocx_LogsArchitectureDocxExportGenerated()
    {
        string runId = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        Mock<IRunDetailQueryService> runDetail = new();
        runDetail.Setup(r => r.GetRunDetailAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ArchitectureRunDetail { Run = new ArchitectureRun { RunId = runId, Status = ArchitectureRunStatus.Committed } });

        Mock<IArchitectureAnalysisService> analysis = new();
        ArchitectureAnalysisReport report = new();
        analysis.Setup(a => a.BuildAsync(It.IsAny<ArchitectureAnalysisRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(
            report);

        Mock<IArchitectureAnalysisDocxExportService> docx = new();
        docx.Setup(d => d.GenerateDocxAsync(report, It.IsAny<CancellationToken>())).ReturnsAsync([1, 2, 3]);

        Mock<IArchitectureAnalysisConsultingDocxExportService> consulting = new();
        Mock<IAuditService> audit = new();

        BackgroundJobWorkUnitExecutor sut = new(
            runDetail.Object,
            analysis.Object,
            docx.Object,
            consulting.Object,
            audit.Object);

        AnalysisReportDocxWorkUnit unit = new(
            new AnalysisReportDocxJobPayload { RunId = runId },
            "out.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        BackgroundJobFile file = await sut.ExecuteAsync(unit, CancellationToken.None);

        file.Content.Should().Equal(1, 2, 3);
        audit.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e =>
                    e.EventType == AuditEventTypes.ArchitectureDocxExportGenerated &&
                    e.RunId == Guid.ParseExact(runId, "N") &&
                    !string.IsNullOrWhiteSpace(e.CorrelationId) &&
                    e.CorrelationId.StartsWith("analysis-report-docx-async:", StringComparison.Ordinal) &&
                    e.OccurredUtc <= DateTime.UtcNow),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ConsultingDocx_LogsArchitectureDocxExportGenerated()
    {
        string runId = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
        Mock<IRunDetailQueryService> runDetail = new();
        runDetail.Setup(r => r.GetRunDetailAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ArchitectureRunDetail { Run = new ArchitectureRun { RunId = runId, Status = ArchitectureRunStatus.Committed } });

        Mock<IArchitectureAnalysisService> analysis = new();
        ArchitectureAnalysisReport report = new();
        analysis.Setup(a => a.BuildAsync(It.IsAny<ArchitectureAnalysisRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(
            report);

        Mock<IArchitectureAnalysisDocxExportService> docx = new();
        Mock<IArchitectureAnalysisConsultingDocxExportService> consulting = new();
        consulting.Setup(c => c.GenerateDocxAsync(report, It.IsAny<CancellationToken>())).ReturnsAsync([9]);

        Mock<IAuditService> audit = new();

        BackgroundJobWorkUnitExecutor sut = new(
            runDetail.Object,
            analysis.Object,
            docx.Object,
            consulting.Object,
            audit.Object);

        ConsultingDocxWorkUnit unit = new(
            new ConsultingDocxJobPayload { RunId = runId },
            "consult.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        BackgroundJobFile file = await sut.ExecuteAsync(unit, CancellationToken.None);

        file.Content.Should().Equal(9);
        audit.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e =>
                    e.EventType == AuditEventTypes.ArchitectureDocxExportGenerated &&
                    e.CorrelationId.StartsWith("analysis-report-consulting-docx-async:", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
