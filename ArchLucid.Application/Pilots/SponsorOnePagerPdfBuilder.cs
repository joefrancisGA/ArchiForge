using System.Globalization;

using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Explanation;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using QuestPdfDocument = QuestPDF.Fluent.Document;

namespace ArchLucid.Application.Pilots;

/// <summary>
/// One-page QuestPDF sponsor summary keyed to a single run plus tenant pilot scorecard aggregates (read-only).
/// </summary>
/// <remarks>
/// The headline numbers come from <see cref="IPilotRunDeltaComputer"/> so the sponsor PDF, the Markdown sibling
/// (<see cref="FirstValueReportBuilder"/>), and the canonical first-value-report PDF wrapper all show the same
/// computed deltas. When the run is a Contoso Retail demo seed the page is stamped "demo tenant — replace before
/// publishing" so the seeded numbers cannot be quoted as a real-customer outcome.
/// </remarks>
public sealed class SponsorOnePagerPdfBuilder(
    IRunDetailQueryService runDetailQuery,
    PilotScorecardBuilder scorecardBuilder,
    IPilotRunDeltaComputer deltaComputer)
{
    private const string DemoTenantBanner = "demo tenant — replace before publishing";

    private readonly IRunDetailQueryService _runDetailQuery =
        runDetailQuery ?? throw new ArgumentNullException(nameof(runDetailQuery));

    private readonly PilotScorecardBuilder _scorecardBuilder =
        scorecardBuilder ?? throw new ArgumentNullException(nameof(scorecardBuilder));

    private readonly IPilotRunDeltaComputer _deltaComputer =
        deltaComputer ?? throw new ArgumentNullException(nameof(deltaComputer));

    /// <summary>Returns PDF bytes, or <see langword="null"/> when the run is missing.</summary>
    public async Task<byte[]?> BuildPdfAsync(
        string runId,
        string baseUrlForFooter,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("Run id is required.", nameof(runId));

        ArchitectureRunDetail? detail = await _runDetailQuery.GetRunDetailAsync(runId, cancellationToken);

        if (detail is null)
            return null;

        PilotRunDeltas deltas = await _deltaComputer.ComputeAsync(detail, cancellationToken);

        DateTimeOffset end = DateTimeOffset.UtcNow;
        DateTimeOffset start = end.AddDays(-30);
        PilotScorecardSummary scorecard = await _scorecardBuilder.BuildAsync(start, end, cancellationToken);

        QuestPDF.Settings.License = LicenseType.Community;

        ArchitectureRun run = detail.Run;
        GoldenManifest? manifest = detail.Manifest;
        string footer = string.IsNullOrWhiteSpace(baseUrlForFooter)
            ? "http://localhost:5000"
            : baseUrlForFooter.Trim().TrimEnd('/');

        int denom = Math.Max(1, scorecard.RunsInPeriod);
        double committedRatio = scorecard.RunsWithCommittedManifest / (double)denom;

        QuestPdfDocument doc = QuestPdfDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Helvetica"));

                page.Header().Text("ArchLucid — sponsor one-pager (pilot)").Bold().FontSize(14);
                page.Content().Column(column =>
                {
                    column.Item().Text($"Run: {run.RunId}").FontSize(11);
                    column.Item().Text($"Generated (UTC): {DateTime.UtcNow:O}");

                    if (deltas.IsDemoTenant)

                        column.Item().PaddingTop(4).Background(Colors.Yellow.Lighten3).Padding(4)
                            .Text(DemoTenantBanner).Bold().FontColor(Colors.Red.Darken2);


                    column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    column.Item().PaddingTop(8).Text("Computed deltas (this run)").Bold().FontSize(12);
                    column.Item().Element(c => RenderComputedDeltasTable(c, deltas));

                    column.Item().PaddingTop(4).Text(
                        $"Pilot window (last 30 days): {scorecard.RunsWithCommittedManifest} committed runs / {scorecard.RunsInPeriod} runs in scope ({committedRatio:P0}).");

                    if (manifest is not null)
                    {
                        column.Item().PaddingTop(4).Text($"Committed manifest version: {manifest.Metadata.ManifestVersion}");
                        column.Item().Text($"System: {manifest.SystemName}");
                    }
                    else
                    {
                        column.Item().PaddingTop(4).Text("Committed manifest: not available on this run (fill baseline per docs/PILOT_ROI_MODEL.md).");
                    }

                    column.Item().PaddingTop(12).Text("Top-severity finding — evidence chain").Bold().FontSize(12);
                    column.Item().Element(c => RenderEvidenceChain(c, deltas));

                    column.Item().PaddingTop(12).Text("Illustrative waterfall (pilot activity mix)").Bold().FontSize(12);
                    column.Item().Text("Bar heights are proportional to: total runs in window, committed runs, remainder (not dollar estimates).");
                    column.Item().PaddingTop(6).Row(row =>
                    {
                        float h1 = 80f;
                        float h2 = (float)(committedRatio * 80);
                        float h3 = (float)Math.Max(4, 80 - h2);

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Height(h1, Unit.Point).Background(Colors.Grey.Lighten3);
                            c.Item().Text("Runs in window").FontSize(8).AlignCenter();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Height(h2, Unit.Point).Background(Colors.Blue.Lighten3);
                            c.Item().Text("Committed").FontSize(8).AlignCenter();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Height(h3, Unit.Point).Background(Colors.Green.Lighten3);
                            c.Item().Text("Other").FontSize(8).AlignCenter();
                        });
                    });

                    column.Item().PaddingTop(12).Text("Canonical narrative").Bold();
                    column.Item().Text("Repository docs/EXECUTIVE_SPONSOR_BRIEF.md and docs/go-to-market/ROI_MODEL.md — this PDF is a pointer, not a substitute for those documents.");

                    column.Item().PaddingTop(10).Text($"Deep link: {footer}/v1/architecture/run/{run.RunId}");
                });
            });
        });

        using MemoryStream stream = new();
        doc.GeneratePdf(stream);

        return stream.ToArray();
    }

    /// <summary>Renders the four computed-delta rows (time-to-commit, findings total, LLM calls, audit rows) as a 2-column table.</summary>
    private static void RenderComputedDeltasTable(QuestPDF.Infrastructure.IContainer column, PilotRunDeltas deltas)
    {
        column.PaddingTop(4).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2);
                c.RelativeColumn(3);
            });

            AddDeltaRow(table, "Time to committed manifest", FormatTimeToCommit(deltas));
            AddDeltaRow(table, "Findings (total)", deltas.FindingsBySeverity.Sum(static p => p.Value).ToString(CultureInfo.InvariantCulture));
            AddDeltaRow(table, "Findings by severity", FormatFindingsBySeverity(deltas));
            AddDeltaRow(table, "LLM calls for this run", deltas.LlmCallCount.ToString(CultureInfo.InvariantCulture));
            AddDeltaRow(table, "Audit rows for this run", FormatAuditRows(deltas));
        });
    }

    private static void AddDeltaRow(QuestPDF.Fluent.TableDescriptor table, string label, string value)
    {
        table.Cell().Padding(2).Text(label);
        table.Cell().Padding(2).Text(value).Bold();
    }

    private static string FormatTimeToCommit(PilotRunDeltas deltas)
    {
        if (deltas.TimeToCommittedManifest is not { } wall)
            return "(pending — no committed manifest)";

        return $"{wall:c} (committed {deltas.ManifestCommittedUtc:O})";
    }

    private static string FormatFindingsBySeverity(PilotRunDeltas deltas)
    {
        if (deltas.FindingsBySeverity.Count == 0)
            return "(none)";

        return string.Join(
            ", ",
            deltas.FindingsBySeverity.Select(static p => $"{p.Key}: {p.Value}"));
    }

    private static string FormatAuditRows(PilotRunDeltas deltas)
    {
        if (deltas.AuditRowCount == 0)
            return "0";

        return deltas.AuditRowCountTruncated
            ? $"{deltas.AuditRowCount}+ (cap reached)"
            : deltas.AuditRowCount.ToString(CultureInfo.InvariantCulture);
    }

    private static void RenderEvidenceChain(QuestPDF.Infrastructure.IContainer column, PilotRunDeltas deltas)
    {
        if (deltas.TopFindingId is null)
        {
            column.Text("(No findings on this run; evidence-chain excerpt skipped.)").Italic();

            return;
        }

        column.Column(c =>
        {
            c.Item().Text($"Selected finding: {deltas.TopFindingId} (severity {deltas.TopFindingSeverity ?? "Unknown"}).");

            FindingEvidenceChainResponse? chain = deltas.TopFindingEvidenceChain;

            if (chain is null)
            {
                c.Item().Text("(Evidence chain unavailable — finding not present in persisted FindingsSnapshot.)").Italic();

                return;
            }

            c.Item().Text($"Manifest version: {chain.ManifestVersion ?? "(none)"}");
            c.Item().Text($"Findings snapshot: {FormatGuid(chain.FindingsSnapshotId)} · context: {FormatGuid(chain.ContextSnapshotId)}");
            c.Item().Text($"Graph snapshot: {FormatGuid(chain.GraphSnapshotId)} · decision trace: {FormatGuid(chain.DecisionTraceId)}");
            c.Item().Text($"Golden manifest: {FormatGuid(chain.GoldenManifestId)}");
            c.Item().Text($"Related graph nodes: {chain.RelatedGraphNodeIds.Count} · agent execution traces: {chain.AgentExecutionTraceIds.Count}");
        });
    }

    private static string FormatGuid(Guid? id) => id is null ? "(none)" : id.Value.ToString("D");
}
