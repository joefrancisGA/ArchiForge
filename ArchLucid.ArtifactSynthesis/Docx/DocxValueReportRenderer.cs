using ArchLucid.Contracts.ValueReports;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

using Microsoft.Extensions.Logging;

namespace ArchLucid.ArtifactSynthesis.Docx;

/// <summary>
/// Lightweight OpenXML document for sponsor value metrics (not the architecture-package <see cref="DocxExportService"/> pipeline).
/// </summary>
public sealed class DocxValueReportRenderer(ILogger<DocxValueReportRenderer> logger) : IValueReportRenderer
{
    private readonly ILogger<DocxValueReportRenderer> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public Task<byte[]> RenderAsync(ValueReportSnapshot snapshot, CancellationToken cancellationToken)
    {
        if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));

        _logger.LogInformation(
            "Rendering value report DOCX for tenant {TenantId} window {From:o}–{To:o}.",
            snapshot.TenantId,
            snapshot.PeriodFromUtc,
            snapshot.PeriodToUtc);

        using MemoryStream stream = new();

        using (WordprocessingDocument doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            MainDocumentPart main = doc.AddMainDocumentPart();
            main.Document = new Document();
            Body body = main.Document.AppendChild(new Body());

            body.AppendChild(Paragraph("ArchLucid — tenant value report", bold: true, fontSizeHalfPoints: 32));
            body.AppendChild(Paragraph("Stakeholder summary (automated)", bold: false, fontSizeHalfPoints: 24));
            body.AppendChild(Paragraph($"Tenant: {snapshot.TenantId:D}"));
            body.AppendChild(Paragraph($"Workspace: {snapshot.WorkspaceId:D} · Project: {snapshot.ProjectId:D}"));
            body.AppendChild(
                Paragraph(
                    $"Reporting window (UTC): {snapshot.PeriodFromUtc:O} → {snapshot.PeriodToUtc:O}"));

            body.AppendChild(Paragraph("Observed activity", bold: true, fontSizeHalfPoints: 28));
            body.AppendChild(Paragraph($"Runs completed (terminal): {snapshot.RunsCompletedCount}"));
            body.AppendChild(Paragraph($"Manifests committed: {snapshot.ManifestsCommittedCount}"));
            body.AppendChild(Paragraph($"Governance-class audit events: {snapshot.GovernanceEventsHandledCount}"));
            body.AppendChild(Paragraph($"Drift / alert-class audit events: {snapshot.DriftAlertEventsCaughtCount}"));

            body.AppendChild(Paragraph("Runs by legacy status (created in window)", bold: true, fontSizeHalfPoints: 28));

            if (snapshot.RunStatusRows.Count == 0)
                body.AppendChild(Paragraph("(No runs created in this window.)"));
            else
            {
                foreach (ValueReportRunStatusRow row in snapshot.RunStatusRows)
                    body.AppendChild(Paragraph($"{row.LegacyRunStatusLabel}: {row.Count}"));
            }

            body.AppendChild(Paragraph("Estimated hours saved (ROI_MODEL inputs)", bold: true, fontSizeHalfPoints: 28));
            body.AppendChild(
                Paragraph(
                    $"From committed manifests: {snapshot.EstimatedArchitectHoursSavedFromManifests:0.##} architect-hours"));
            body.AppendChild(
                Paragraph(
                    $"From governance events: {snapshot.EstimatedArchitectHoursSavedFromGovernanceEvents:0.##} architect-hours"));
            body.AppendChild(
                Paragraph(
                    $"From drift/alert events: {snapshot.EstimatedArchitectHoursSavedFromDriftEvents:0.##} architect-hours"));
            body.AppendChild(Paragraph($"Total (composite): {snapshot.EstimatedTotalArchitectHoursSaved:0.##} architect-hours"));

            body.AppendChild(Paragraph("LLM cost (estimated)", bold: true, fontSizeHalfPoints: 28));
            body.AppendChild(Paragraph($"Window USD (completed runs × model rate): {snapshot.EstimatedLlmCostForWindowUsd:0.##}"));
            body.AppendChild(Paragraph(snapshot.EstimatedLlmCostMethodologyNote));

            body.AppendChild(Paragraph("ROI vs ROI_MODEL.md baseline (annualized)", bold: true, fontSizeHalfPoints: 28));
            body.AppendChild(
                Paragraph(
                    $"Annualized hours value (USD): {snapshot.AnnualizedHoursValueUsd:0.##}"));
            body.AppendChild(Paragraph($"Annualized LLM cost (USD): {snapshot.AnnualizedLlmCostUsd:0.##}"));
            body.AppendChild(
                Paragraph(
                    $"Baseline annual subscription + ops (model doc): {snapshot.BaselineAnnualSubscriptionAndOpsCostUsdFromRoiModel:0.##}"));
            body.AppendChild(
                Paragraph(
                    $"Net annualized value vs baseline (USD): {snapshot.NetAnnualizedValueVersusRoiBaselineUsd:0.##}"));
            body.AppendChild(
                Paragraph(
                    $"ROI vs baseline (%): {snapshot.RoiAnnualizedPercentVersusRoiBaseline:0.##}"));

            body.AppendChild(
                Paragraph(
                    "Figures annualize the observed window to 365 days for comparability with the ROI_MODEL annual totals.",
                    italic: true));
        }

        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(stream.ToArray());
    }

    private static Paragraph Paragraph(
        string text,
        bool bold = false,
        bool italic = false,
        int fontSizeHalfPoints = 22)
    {
        Paragraph p = new();
        Run r = new();
        RunProperties props = new();
        props.AppendChild(new FontSize { Val = fontSizeHalfPoints.ToString() });

        if (bold)
            props.AppendChild(new Bold());

        if (italic)
            props.AppendChild(new Italic());

        r.AppendChild(props);
        r.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        p.AppendChild(r);

        return p;
    }
}
