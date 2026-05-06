using ArchLucid.Application.Analysis;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Core.Audit;
using ArchLucid.Persistence.Data.Repositories;

using FluentAssertions;

using Moq;

namespace ArchLucid.Application.Tests.Analysis;

[Trait("Category", "Unit")]
public sealed class RunExportAuditServiceTests
{
    [Fact]
    public async Task RecordAsync_WhenEmitAuditTrue_LogsArchitectureDocxExportGenerated()
    {
        Mock<IRunExportRecordRepository> repo = new();
        Mock<IAuditService> audit = new();
        RunExportAuditService sut = new(repo.Object, audit.Object);
        string runId = "deadbeefdeadbeefdeadbeefdeadbeef";

        RunExportRecord row = await sut.RecordAsync(
            runId,
            "analysis-report-consulting-docx",
            "docx",
            "file.docx",
            "executive",
            "Executive",
            false,
            null,
            "mv1",
            emitArchitectureDocxExportGeneratedAudit: true,
            cancellationToken: CancellationToken.None);

        row.ExportRecordId.Should().NotBeNullOrWhiteSpace();
        audit.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e =>
                    e.EventType == AuditEventTypes.ArchitectureDocxExportGenerated &&
                    e.RunId == Guid.ParseExact(runId, "N") &&
                    e.OccurredUtc <= DateTime.UtcNow &&
                    e.DataJson.Contains(row.ExportRecordId, StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordAsync_WhenEmitAuditFalse_SkipsDurableDocxExportRow()
    {
        Mock<IRunExportRecordRepository> repo = new();
        Mock<IAuditService> audit = new();
        RunExportAuditService sut = new(repo.Object, audit.Object);

        await sut.RecordAsync(
            "a",
            "analysis-report-docx",
            "docx",
            "f.docx",
            null,
            null,
            false,
            null,
            null,
            emitArchitectureDocxExportGeneratedAudit: false,
            cancellationToken: CancellationToken.None);

        audit.Verify(
            a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
