using System.Text.Json;

using ArchiForge.Application.Analysis;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Persistence.Data.Repositories;

using FluentAssertions;

using Moq;

namespace ArchiForge.Api.Tests;

/// <summary>
/// <see cref="ExportReplayService.ReplayAsync"/> contract: validation, rehydration failures, unsupported export types,
/// and happy paths for <c>analysis-report-consulting-docx</c> vs <c>analysis-report-docx</c>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ExportReplayServiceReplayAsyncTests
{
    private static readonly JsonSerializerOptions PersistJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    [Fact]
    public async Task ReplayAsync_NullRequest_ThrowsArgumentNullException()
    {
        ExportReplayService sut = CreateSut(out _, out _, out _, out _, out _);

        Func<Task> act = async () => await sut.ReplayAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ReplayAsync_BlankExportRecordId_ThrowsArgumentException()
    {
        ExportReplayService sut = CreateSut(out _, out _, out _, out _, out _);

        Func<Task> act = async () =>
            await sut.ReplayAsync(new ReplayExportRequest { ExportRecordId = "   " });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ReplayAsync_RecordNotFound_ThrowsInvalidOperationException()
    {
        ExportReplayService sut = CreateSut(out Mock<IRunExportRecordRepository> repo, out _, out _, out _, out _);
        repo.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RunExportRecord?)null);

        Func<Task> act = async () =>
            await sut.ReplayAsync(new ReplayExportRequest { ExportRecordId = "missing" });

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("missing");
    }

    [Fact]
    public async Task ReplayAsync_MissingAnalysisRequestJson_ThrowsInvalidOperationException()
    {
        ExportReplayService sut = CreateSut(out Mock<IRunExportRecordRepository> repo, out _, out _, out _, out _);
        RunExportRecord record = BaseRecord("analysis-report-consulting-docx");
        record.AnalysisRequestJson = null;
        repo.Setup(r => r.GetByIdAsync(record.ExportRecordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        Func<Task> act = async () =>
            await sut.ReplayAsync(new ReplayExportRequest { ExportRecordId = record.ExportRecordId });

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("does not contain a persisted analysis request");
    }

    [Fact]
    public async Task ReplayAsync_CorruptAnalysisRequestJson_ThrowsInvalidOperationException_WithJsonExceptionInner()
    {
        ExportReplayService sut = CreateSut(out Mock<IRunExportRecordRepository> repo, out _, out _, out _, out _);
        RunExportRecord record = BaseRecord("analysis-report-consulting-docx");
        record.AnalysisRequestJson = "{";
        repo.Setup(r => r.GetByIdAsync(record.ExportRecordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        Func<Task> act = async () =>
            await sut.ReplayAsync(new ReplayExportRequest { ExportRecordId = record.ExportRecordId });

        Exception ex = (await act.Should().ThrowAsync<InvalidOperationException>()).Which;
        ex.Message.Should().Contain("could not be deserialized");
        ex.InnerException.Should().BeOfType<JsonException>();
    }

    [Fact]
    public async Task ReplayAsync_UnsupportedExportType_ThrowsInvalidOperationException()
    {
        ExportReplayService sut = CreateSut(out Mock<IRunExportRecordRepository> repo, out _, out _, out _, out _);
        RunExportRecord record = BaseRecord("ArchitectureAnalysis");
        record.AnalysisRequestJson = JsonSerializer.Serialize(MinimalPersistedRequest(), PersistJsonOptions);
        repo.Setup(r => r.GetByIdAsync(record.ExportRecordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        Func<Task> act = async () =>
            await sut.ReplayAsync(new ReplayExportRequest { ExportRecordId = record.ExportRecordId });

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("Replay is not supported");
    }

    [Fact]
    public async Task ReplayAsync_ConsultingDocx_UsesConsultingGenerator_NotStandardDocx()
    {
        ExportReplayService sut = CreateSut(
            out Mock<IRunExportRecordRepository> repo,
            out Mock<IArchitectureAnalysisService> analysis,
            out Mock<IArchitectureAnalysisDocxExportService> standardDocx,
            out Mock<IArchitectureAnalysisConsultingDocxExportService> consultingDocx,
            out _);

        RunExportRecord record = BaseRecord("analysis-report-consulting-docx");
        record.AnalysisRequestJson = JsonSerializer.Serialize(MinimalPersistedRequest(), PersistJsonOptions);
        repo.Setup(r => r.GetByIdAsync(record.ExportRecordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        ArchitectureAnalysisReport built = new();
        analysis.Setup(a => a.BuildAsync(It.IsAny<ArchitectureAnalysisRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(built);
        consultingDocx.Setup(c => c.GenerateDocxAsync(built, It.IsAny<CancellationToken>()))
            .ReturnsAsync([1, 2, 3]);

        ReplayExportResult result = await sut.ReplayAsync(new ReplayExportRequest { ExportRecordId = record.ExportRecordId });

        result.Content.Should().Equal(1, 2, 3);
        result.FileName.Should().Be("base_replay.docx");
        consultingDocx.Verify(
            c => c.GenerateDocxAsync(built, It.IsAny<CancellationToken>()),
            Times.Once);
        standardDocx.Verify(
            s => s.GenerateDocxAsync(It.IsAny<ArchitectureAnalysisReport>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReplayAsync_AnalysisDocx_UsesStandardGenerator_NotConsulting()
    {
        ExportReplayService sut = CreateSut(
            out Mock<IRunExportRecordRepository> repo,
            out Mock<IArchitectureAnalysisService> analysis,
            out Mock<IArchitectureAnalysisDocxExportService> standardDocx,
            out Mock<IArchitectureAnalysisConsultingDocxExportService> consultingDocx,
            out _);

        RunExportRecord record = BaseRecord("analysis-report-docx");
        record.AnalysisRequestJson = JsonSerializer.Serialize(MinimalPersistedRequest(), PersistJsonOptions);
        repo.Setup(r => r.GetByIdAsync(record.ExportRecordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        ArchitectureAnalysisReport built = new();
        analysis.Setup(a => a.BuildAsync(It.IsAny<ArchitectureAnalysisRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(built);
        standardDocx.Setup(s => s.GenerateDocxAsync(built, It.IsAny<CancellationToken>()))
            .ReturnsAsync([9]);

        ReplayExportResult result = await sut.ReplayAsync(new ReplayExportRequest { ExportRecordId = record.ExportRecordId });

        result.Content.Should().Equal("\t"u8.ToArray());
        standardDocx.Verify(
            s => s.GenerateDocxAsync(built, It.IsAny<CancellationToken>()),
            Times.Once);
        consultingDocx.Verify(
            c => c.GenerateDocxAsync(It.IsAny<ArchitectureAnalysisReport>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReplayAsync_WhenRecordReplayExportTrue_RecordsAuditOnce()
    {
        ExportReplayService sut = CreateSut(
            out Mock<IRunExportRecordRepository> repo,
            out Mock<IArchitectureAnalysisService> analysis,
            out Mock<IArchitectureAnalysisDocxExportService> standardDocx,
            out Mock<IArchitectureAnalysisConsultingDocxExportService> consultingDocx,
            out Mock<IRunExportAuditService> audit);

        RunExportRecord record = BaseRecord("analysis-report-docx");
        record.AnalysisRequestJson = JsonSerializer.Serialize(MinimalPersistedRequest(), PersistJsonOptions);
        repo.Setup(r => r.GetByIdAsync(record.ExportRecordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        ArchitectureAnalysisReport built = new();
        analysis.Setup(a => a.BuildAsync(It.IsAny<ArchitectureAnalysisRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(built);
        standardDocx.Setup(s => s.GenerateDocxAsync(built, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await sut.ReplayAsync(
            new ReplayExportRequest
            {
                ExportRecordId = record.ExportRecordId,
                RecordReplayExport = true
            });

        audit.Verify(
            a => a.RecordAsync(
                record.RunId,
                record.ExportType,
                record.Format,
                It.IsAny<string>(),
                record.TemplateProfile,
                record.TemplateProfileDisplayName,
                record.WasAutoSelected,
                record.ResolutionReason,
                It.IsAny<string?>(),
                It.IsAny<PersistedAnalysisExportRequest?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        consultingDocx.Verify(
            c => c.GenerateDocxAsync(It.IsAny<ArchitectureAnalysisReport>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static ExportReplayService CreateSut(
        out Mock<IRunExportRecordRepository> repo,
        out Mock<IArchitectureAnalysisService> analysis,
        out Mock<IArchitectureAnalysisDocxExportService> standardDocx,
        out Mock<IArchitectureAnalysisConsultingDocxExportService> consultingDocx,
        out Mock<IRunExportAuditService> audit)
    {
        repo = new Mock<IRunExportRecordRepository>();
        analysis = new Mock<IArchitectureAnalysisService>();
        standardDocx = new Mock<IArchitectureAnalysisDocxExportService>();
        consultingDocx = new Mock<IArchitectureAnalysisConsultingDocxExportService>();
        audit = new Mock<IRunExportAuditService>();

        return new ExportReplayService(
            repo.Object,
            analysis.Object,
            standardDocx.Object,
            consultingDocx.Object,
            audit.Object);
    }

    private static RunExportRecord BaseRecord(string exportType)
    {
        return new RunExportRecord
        {
            ExportRecordId = Guid.NewGuid().ToString("N"),
            RunId = "run-replay-test",
            ExportType = exportType,
            Format = "docx",
            FileName = "base.docx",
            TemplateProfile = "executive",
            TemplateProfileDisplayName = "Executive",
            WasAutoSelected = false,
            ResolutionReason = "test",
            CreatedUtc = DateTime.UtcNow
        };
    }

    private static PersistedAnalysisExportRequest MinimalPersistedRequest()
    {
        return new PersistedAnalysisExportRequest
        {
            IncludeEvidence = false,
            IncludeExecutionTraces = false,
            IncludeManifest = true,
            IncludeDiagram = false,
            IncludeSummary = true,
            IncludeDeterminismCheck = false,
            DeterminismIterations = 0,
            IncludeManifestCompare = false,
            IncludeAgentResultCompare = false
        };
    }
}
