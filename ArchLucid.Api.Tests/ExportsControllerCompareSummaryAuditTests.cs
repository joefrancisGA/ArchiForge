using System.Security.Claims;

using ArchLucid.Api.Controllers.Authority;
using ArchLucid.Api.Models;
using ArchLucid.Application;
using ArchLucid.Application.Analysis;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Core.Audit;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Durable audit for <c>POST .../run/exports/compare/summary</c> when <c>persist: true</c>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ExportsControllerCompareSummaryAuditTests
{
    [Fact]
    public async Task CompareExportRecordsSummary_WhenPersistTrue_LogsComparisonSummaryPersisted()
    {
        string leftId = "exp-left";
        string rightId = "exp-right";
        string comparisonId = "cmp-99";

        Mock<IRunExportRecordRepository> runExports = new();
        runExports.Setup(r => r.GetByIdAsync(leftId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RunExportRecord { ExportRecordId = leftId });
        runExports.Setup(r => r.GetByIdAsync(rightId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RunExportRecord { ExportRecordId = rightId });

        Mock<IExportRecordDiffService> diffService = new();
        diffService.Setup(d => d.Compare(It.IsAny<RunExportRecord>(), It.IsAny<RunExportRecord>()))
            .Returns(new ExportRecordDiffResult());

        Mock<IExportRecordDiffSummaryFormatter> formatter = new();
        formatter.Setup(f => f.FormatMarkdown(It.IsAny<ExportRecordDiffResult>()))
            .Returns("# diff");

        Mock<IComparisonAuditService> comparisonAudit = new();
        comparisonAudit
            .Setup(c => c.RecordExportDiffAsync(It.IsAny<ExportRecordDiffResult>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(comparisonId);

        Mock<IAuditService> audit = new();

        ExportsController sut = new(
            Mock.Of<IRunDetailQueryService>(),
            runExports.Object,
            comparisonAudit.Object,
            Mock.Of<IExportReplayService>(),
            diffService.Object,
            formatter.Object,
            audit.Object);

        DefaultHttpContext http = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "u1")]))
        };
        sut.ControllerContext = new ControllerContext { HttpContext = http };

        IActionResult result = await sut.CompareExportRecordsSummary(
            leftId,
            rightId,
            new PersistComparisonRequest { Persist = true },
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        audit.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e =>
                    e.EventType == AuditEventTypes.ComparisonSummaryPersisted
                    && e.DataJson.Contains(comparisonId, StringComparison.Ordinal)
                    && e.DataJson.Contains(leftId, StringComparison.Ordinal)
                    && e.DataJson.Contains(rightId, StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CompareExportRecordsSummary_WhenPersistFalse_DoesNotLogComparisonSummaryPersisted()
    {
        string leftId = "exp-a";
        string rightId = "exp-b";

        Mock<IRunExportRecordRepository> runExports = new();
        runExports.Setup(r => r.GetByIdAsync(leftId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RunExportRecord { ExportRecordId = leftId });
        runExports.Setup(r => r.GetByIdAsync(rightId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RunExportRecord { ExportRecordId = rightId });

        Mock<IExportRecordDiffService> diffService = new();
        diffService.Setup(d => d.Compare(It.IsAny<RunExportRecord>(), It.IsAny<RunExportRecord>()))
            .Returns(new ExportRecordDiffResult());

        Mock<IExportRecordDiffSummaryFormatter> formatter = new();
        formatter.Setup(f => f.FormatMarkdown(It.IsAny<ExportRecordDiffResult>()))
            .Returns("# diff");

        Mock<IAuditService> audit = new();

        ExportsController sut = new(
            Mock.Of<IRunDetailQueryService>(),
            runExports.Object,
            Mock.Of<IComparisonAuditService>(),
            Mock.Of<IExportReplayService>(),
            diffService.Object,
            formatter.Object,
            audit.Object);

        DefaultHttpContext http = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "u1")]))
        };
        sut.ControllerContext = new ControllerContext { HttpContext = http };

        IActionResult result = await sut.CompareExportRecordsSummary(
            leftId,
            rightId,
            new PersistComparisonRequest { Persist = false },
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        audit.Verify(
            a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
