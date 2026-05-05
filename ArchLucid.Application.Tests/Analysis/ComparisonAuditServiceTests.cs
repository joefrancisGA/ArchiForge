using ArchLucid.Application.Analysis;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Core.Audit;

using FluentAssertions;

using Moq;

namespace ArchLucid.Application.Tests.Analysis;

[Trait("Category", "Unit")]
public sealed class ComparisonAuditServiceTests
{
    [Fact]
    public async Task RecordEndToEndAsync_AfterCreate_LogsEndToEndComparisonPersisted()
    {
        Mock<IComparisonRecordRepository> repo = new();
        Mock<IAuditService> audit = new();
        ComparisonAuditService sut = new(repo.Object, audit.Object);
        EndToEndReplayComparisonReport report = new()
        {
            LeftRunId = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            RightRunId = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"
        };
        const string summary = "# Summary";

        string id = await sut.RecordEndToEndAsync(report, summary, CancellationToken.None);

        id.Should().NotBeNullOrWhiteSpace();
        repo.Verify(r => r.CreateAsync(It.IsAny<ComparisonRecord>(), It.IsAny<CancellationToken>()), Times.Once);
        audit.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e =>
                    e.EventType == AuditEventTypes.EndToEndComparisonPersisted &&
                    e.RunId == Guid.ParseExact("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "N") &&
                    e.DataJson.Contains(id, StringComparison.Ordinal) &&
                    e.DataJson.Contains(report.LeftRunId, StringComparison.Ordinal) &&
                    e.OccurredUtc <= DateTime.UtcNow),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordReplayOfAsync_AfterCreate_LogsComparisonReplayPersisted()
    {
        Mock<IComparisonRecordRepository> repo = new();
        Mock<IAuditService> audit = new();
        ComparisonAuditService sut = new(repo.Object, audit.Object);
        ComparisonRecord source = new()
        {
            ComparisonRecordId = "cccccccccccccccccccccccccccccccc",
            ComparisonType = ComparisonTypes.EndToEndReplay,
            LeftRunId = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            RightRunId = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
            Format = ComparisonTypes.FormatJsonMarkdown,
            SummaryMarkdown = "x",
            PayloadJson = "{}",
            CreatedUtc = DateTime.UtcNow
        };

        string newId = await sut.RecordReplayOfAsync(source, "replay note", CancellationToken.None);

        newId.Should().NotBeNullOrWhiteSpace();
        repo.Verify(r => r.CreateAsync(It.IsAny<ComparisonRecord>(), It.IsAny<CancellationToken>()), Times.Once);
        audit.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e =>
                    e.EventType == AuditEventTypes.ComparisonReplayPersisted &&
                    e.RunId == Guid.ParseExact("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "N") &&
                    e.DataJson.Contains(newId, StringComparison.Ordinal) &&
                    e.DataJson.Contains(source.ComparisonRecordId, StringComparison.Ordinal) &&
                    e.OccurredUtc <= DateTime.UtcNow),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordExportDiffAsync_AfterCreate_DoesNotLogDuplicateDurableRow()
    {
        Mock<IComparisonRecordRepository> repo = new();
        Mock<IAuditService> audit = new();
        ComparisonAuditService sut = new(repo.Object, audit.Object);
        ExportRecordDiffResult diff = new()
        {
            LeftExportRecordId = "l1",
            RightExportRecordId = "r1",
            LeftRunId = "lr",
            RightRunId = "rr"
        };

        await sut.RecordExportDiffAsync(diff, "# md", CancellationToken.None);

        audit.Verify(
            a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
