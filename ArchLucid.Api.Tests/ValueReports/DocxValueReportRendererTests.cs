using System.IO.Compression;
using System.Text;

using ArchLucid.ArtifactSynthesis.Docx;
using ArchLucid.Contracts.ValueReports;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

namespace ArchLucid.Api.Tests.ValueReports;

public sealed class DocxValueReportRendererTests
{
    [Fact]
    public async Task RenderAsync_includes_key_roi_sections_in_document_xml()
    {
        DocxValueReportRenderer sut = new(NullLogger<DocxValueReportRenderer>.Instance);
        ValueReportSnapshot snapshot = new(
            TenantId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            WorkspaceId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ProjectId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            PeriodFromUtc: DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            PeriodToUtc: DateTimeOffset.Parse("2026-01-08T00:00:00Z"),
            RunStatusRows: [new ValueReportRunStatusRow("Completed", 2)],
            RunsCompletedCount: 2,
            ManifestsCommittedCount: 1,
            GovernanceEventsHandledCount: 3,
            DriftAlertEventsCaughtCount: 4,
            EstimatedArchitectHoursSavedFromManifests: 4m,
            EstimatedArchitectHoursSavedFromGovernanceEvents: 1.5m,
            EstimatedArchitectHoursSavedFromDriftEvents: 1m,
            EstimatedTotalArchitectHoursSaved: 6.5m,
            EstimatedLlmCostForWindowUsd: 10m,
            EstimatedLlmCostMethodologyNote: "Test note.",
            AnnualizedHoursValueUsd: 100_000m,
            AnnualizedLlmCostUsd: 500m,
            BaselineAnnualSubscriptionAndOpsCostUsdFromRoiModel: 27_360m,
            NetAnnualizedValueVersusRoiBaselineUsd: 72_140m,
            RoiAnnualizedPercentVersusRoiBaseline: 263.67m,
            TenantBaselineReviewCycleHours: null,
            TenantBaselineReviewCycleSource: null,
            TenantBaselineReviewCycleCapturedUtc: null,
            MeasuredAverageReviewCycleHoursForWindow: null,
            MeasuredReviewCycleSampleSize: 0,
            ReviewCycleBaselineProvenance: ReviewCycleBaselineProvenance.NoMeasurementYet,
            ReviewCycleHoursDelta: null,
            ReviewCycleHoursDeltaPercent: null);

        byte[] docx = await sut.RenderAsync(snapshot, CancellationToken.None);

        docx.Should().NotBeEmpty();

        using MemoryStream ms = new(docx);
        using ZipArchive zip = new(ms, ZipArchiveMode.Read);
        ZipArchiveEntry? entry = zip.GetEntry("word/document.xml");
        entry.Should().NotBeNull();

        using StreamReader reader = new(entry.Open(), Encoding.UTF8);
        string xml = await reader.ReadToEndAsync();

        xml.Should().Contain("ArchLucid — tenant value report");
        xml.Should().Contain("ROI vs ROI_MODEL.md baseline");
        xml.Should().Contain("Governance-class audit events");
        xml.Should().Contain("Drift / alert-class audit events");
    }
}
