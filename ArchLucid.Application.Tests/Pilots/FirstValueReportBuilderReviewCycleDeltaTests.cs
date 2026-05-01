using System.IO.Compression;
using System.Text;

using ArchLucid.ArtifactSynthesis.Docx;
using ArchLucid.Contracts.ValueReports;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

namespace ArchLucid.Application.Tests.Pilots;

public sealed class FirstValueReportBuilderReviewCycleDeltaTests
{
    [SkippableFact]
    public async Task Markdown_and_DOCX_carry_same_decimal_tokens_for_identical_snapshot()
    {
        ValueReportSnapshot snapshot = new(
            TenantId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            WorkspaceId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ProjectId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            PeriodFromUtc: DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            PeriodToUtc: DateTimeOffset.Parse("2026-01-08T00:00:00Z"),
            RunStatusRows: [],
            RunsCompletedCount: 0,
            ManifestsCommittedCount: 0,
            GovernanceEventsHandledCount: 0,
            DriftAlertEventsCaughtCount: 0,
            EstimatedArchitectHoursSavedFromManifests: 0m,
            EstimatedArchitectHoursSavedFromGovernanceEvents: 0m,
            EstimatedArchitectHoursSavedFromDriftEvents: 0m,
            EstimatedTotalArchitectHoursSaved: 0m,
            EstimatedLlmCostForWindowUsd: 0m,
            EstimatedLlmCostMethodologyNote: "n",
            AnnualizedHoursValueUsd: 0m,
            AnnualizedLlmCostUsd: 0m,
            BaselineAnnualSubscriptionAndOpsCostUsdFromRoiModel: 0m,
            NetAnnualizedValueVersusRoiBaselineUsd: 0m,
            RoiAnnualizedPercentVersusRoiBaseline: 0m,
            TenantBaselineReviewCycleHours: 10m,
            TenantBaselineReviewCycleSource: "src",
            TenantBaselineReviewCycleCapturedUtc: DateTimeOffset.Parse("2026-04-01T12:00:00Z"),
            MeasuredAverageReviewCycleHoursForWindow: 4m,
            MeasuredReviewCycleSampleSize: 2,
            ReviewCycleBaselineProvenance: ReviewCycleBaselineProvenance.TenantSuppliedAtSignup,
            ReviewCycleHoursDelta: 6m,
            ReviewCycleHoursDeltaPercent: 60m,
            FindingFeedbackNetScore: 0,
            FindingFeedbackVoteCount: 0,
            TenantBaselineManualPrepHoursPerReview: null,
            TenantBaselinePeoplePerReview: null);

        StringBuilder sb = new();
        ValueReportReviewCycleSectionFormatter.AppendMarkdownSection(sb, snapshot);
        string markdown = sb.ToString();

        DocxValueReportRenderer renderer = new(NullLogger<DocxValueReportRenderer>.Instance);
        byte[] docx = await renderer.RenderAsync(snapshot, CancellationToken.None);

        string xml = ReadDocumentXml(docx);

        string markdownFlat = markdown.Replace("_", "", StringComparison.Ordinal);

        foreach (ValueReportReviewCycleParagraph p in ValueReportReviewCycleSectionFormatter.GetParagraphs(snapshot))
        {
            if (string.Equals(
                    p.Text,
                    "Review-cycle delta (before vs measured)",
                    StringComparison.Ordinal))
                continue;

            xml.Should().Contain(p.Text);
            markdownFlat.Should().Contain(p.Text);
        }
    }

    private static string ReadDocumentXml(byte[] docx)
    {
        using MemoryStream ms = new(docx);
        using ZipArchive zip = new(ms, ZipArchiveMode.Read);
        ZipArchiveEntry? entry = zip.GetEntry("word/document.xml");
        entry.Should().NotBeNull();

        using StreamReader reader = new(entry.Open(), Encoding.UTF8);

        return reader.ReadToEnd();
    }
}
