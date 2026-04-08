using System.Net;
using System.Text;

using ArchLucid.Application.Diffs;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using QuestPdfDocument = QuestPDF.Fluent.Document;
using WpDocument = DocumentFormat.OpenXml.Wordprocessing.Document;

namespace ArchLucid.Application.Analysis;

/// <summary>
/// Generates exportable artifacts (Markdown, HTML, DOCX, PDF) from an
/// <see cref="EndToEndReplayComparisonReport"/>. Output verbosity is controlled by the
/// <see cref="EndToEndComparisonExportProfile"/> constants (<c>detailed</c>, <c>executive</c>, <c>short</c>).
/// </summary>
public sealed class EndToEndReplayComparisonExportService(IEndToEndReplayComparisonSummaryFormatter summaryFormatter)
    : IEndToEndReplayComparisonExportService
{
    static EndToEndReplayComparisonExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Renders <paramref name="report"/> as a Markdown document under the given export <paramref name="profile"/>.
    /// Defaults to <see cref="EndToEndComparisonExportProfile.Default"/> when <paramref name="profile"/> is <c>null</c>.
    /// </summary>
    public string GenerateMarkdown(EndToEndReplayComparisonReport report, string? profile = null)
    {
        ArgumentNullException.ThrowIfNull(report);
        string p = EndToEndComparisonExportProfile.Normalize(profile);
        StringBuilder sb = new();

        AppendMarkdownHeader(sb, report);
        sb.AppendLine(summaryFormatter.FormatMarkdown(report).Trim());
        sb.AppendLine();

        if (EndToEndComparisonExportProfile.IsShort(p))
        
            return sb.ToString();
        

        sb.AppendLine("---");
        sb.AppendLine();

        if (EndToEndComparisonExportProfile.IsExecutive(p))
        
            AppendMarkdownExecutiveSummary(sb, report);
        
        else
        {
            AppendMarkdownRunMetadataDiff(sb, report);
            AppendMarkdownAgentResultDiff(sb, report);
            AppendMarkdownManifestDiff(sb, report);
            AppendMarkdownExportDiffs(sb, report);
        }

        AppendList(sb, "Interpretation Notes", report.InterpretationNotes);
        AppendList(sb, "Warnings", report.Warnings);

        return sb.ToString();
    }

    /// <summary>
    /// Renders <paramref name="report"/> as a self-contained HTML document under the given export <paramref name="profile"/>.
    /// </summary>
    public string GenerateHtml(EndToEndReplayComparisonReport report, string? profile = null)
    {
        ArgumentNullException.ThrowIfNull(report);
        string p = EndToEndComparisonExportProfile.Normalize(profile);
        StringBuilder sb = new();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head><meta charset=\"utf-8\"/><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"/>");
        sb.AppendLine("<title>ArchLucid End-to-End Replay Comparison</title>");
        sb.AppendLine("<style>body{font-family:system-ui,sans-serif;max-width:900px;margin:1rem auto;padding:0 1rem;}");
        sb.AppendLine("h1{font-size:1.5rem;} h2{font-size:1.2rem;margin-top:1.25rem;} h3{font-size:1rem;}");
        sb.AppendLine("ul{margin:.5rem 0;} li{margin:.25rem 0;} .meta{color:#555;font-size:0.9rem;}</style>");
        sb.AppendLine("</head><body>");

        sb.AppendLine("<h1>ArchLucid End-to-End Replay Comparison Export</h1>");
        sb.AppendLine("<p class=\"meta\">Left Run ID: " + EscapeHtml(report.LeftRunId) + "</p>");
        sb.AppendLine("<p class=\"meta\">Right Run ID: " + EscapeHtml(report.RightRunId) + "</p>");
        sb.AppendLine("<p class=\"meta\">Generated UTC: " + EscapeHtml(DateTime.UtcNow.ToString("O")) + "</p>");
        sb.AppendLine("<p class=\"meta\">Profile: " + EscapeHtml(p) + "</p>");
        sb.AppendLine("<hr/>");

        string summaryHtml = MarkdownToSimpleHtml(summaryFormatter.FormatMarkdown(report).Trim());
        sb.AppendLine(summaryHtml);
        sb.AppendLine();

        if (!EndToEndComparisonExportProfile.IsShort(p))
        {
            sb.AppendLine("<hr/>");
            if (EndToEndComparisonExportProfile.IsExecutive(p))
            {
                AppendHtmlExecutiveSummary(sb, report);
            }
            else
            {
                AppendHtmlRunMetadataDiff(sb, report);
                AppendHtmlAgentResultDiff(sb, report);
                AppendHtmlManifestDiff(sb, report);
                AppendHtmlExportDiffs(sb, report);
            }
            AppendHtmlList(sb, "Interpretation Notes", report.InterpretationNotes);
            AppendHtmlList(sb, "Warnings", report.Warnings);
        }

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    /// <summary>
    /// Renders <paramref name="report"/> as a DOCX byte array using OpenXml under the given export <paramref name="profile"/>.
    /// </summary>
    public Task<byte[]> GenerateDocxAsync(
        EndToEndReplayComparisonReport report,
        CancellationToken cancellationToken = default,
        string? profile = null)
    {
        ArgumentNullException.ThrowIfNull(report);
        string p = EndToEndComparisonExportProfile.Normalize(profile);

        using MemoryStream stream = new();

        using (WordprocessingDocument document = WordprocessingDocument.Create(
                   stream,
                   WordprocessingDocumentType.Document,
                   true))
        {
            MainDocumentPart mainPart = document.AddMainDocumentPart();
            mainPart.Document = new WpDocument(new Body());

            Body body = mainPart.Document.Body!;

            AddHeading(body, "ArchLucid End-to-End Replay Comparison", 1);
            AddParagraph(body, $"Left Run ID: {report.LeftRunId}");
            AddParagraph(body, $"Right Run ID: {report.RightRunId}");
            AddParagraph(body, $"Generated UTC: {DateTime.UtcNow:O}");
            AddParagraph(body, $"Profile: {p}");
            AddSpacer(body);

            if (EndToEndComparisonExportProfile.IsShort(p))
            {
                AddHeading(body, "Summary", 2);
                AddParagraph(body, summaryFormatter.FormatMarkdown(report).Trim());
            }
            else if (EndToEndComparisonExportProfile.IsExecutive(p))
            {
                AddHeading(body, "Summary", 2);
                AddParagraph(body, summaryFormatter.FormatMarkdown(report).Trim());
                AddSpacer(body);
                AddDocxExecutiveSummary(body, report);
            }
            else
            {
                AddHeading(body, "Summary", 2);
                AddParagraph(body, summaryFormatter.FormatMarkdown(report).Trim());
                AddSpacer(body);
                AddHeading(body, "Run Metadata Diff", 2);
                AddBullet(body, $"Request IDs Differ: {(report.RunDiff.RequestIdsDiffer ? "Yes" : "No")}");
                AddBullet(body, $"Manifest Versions Differ: {(report.RunDiff.ManifestVersionsDiffer ? "Yes" : "No")}");
                AddBullet(body, $"Status Differs: {(report.RunDiff.StatusDiffers ? "Yes" : "No")}");
                AddBullet(body, $"Completion State Differs: {(report.RunDiff.CompletionStateDiffers ? "Yes" : "No")}");
                foreach (string field in report.RunDiff.ChangedFields)
                    AddBullet(body, $"Changed Field: {field}");
                AddSpacer(body);

                if (report.AgentResultDiff is not null)
                {
                    AddHeading(body, "Agent Result Diff", 2);
                    foreach (AgentResultDelta delta in report.AgentResultDiff.AgentDeltas.OrderBy(x => x.AgentType))
                    {
                        AddParagraph(body, delta.AgentType.ToString(), bold: true);
                        AddBullet(body, $"Left Exists: {(delta.LeftExists ? "Yes" : "No")}");
                        AddBullet(body, $"Right Exists: {(delta.RightExists ? "Yes" : "No")}");
                        AddBullet(body, $"Left Confidence: {(delta.LeftConfidence.HasValue ? delta.LeftConfidence.Value.ToString("0.00") : "n/a")}");
                        AddBullet(body, $"Right Confidence: {(delta.RightConfidence.HasValue ? delta.RightConfidence.Value.ToString("0.00") : "n/a")}");
                        AddDiffSection(body, "Added Claims", delta.AddedClaims);
                        AddDiffSection(body, "Removed Claims", delta.RemovedClaims);
                        AddDiffSection(body, "Added Findings", delta.AddedFindings);
                        AddDiffSection(body, "Removed Findings", delta.RemovedFindings);
                        AddDiffSection(body, "Added Required Controls", delta.AddedRequiredControls);
                        AddDiffSection(body, "Removed Required Controls", delta.RemovedRequiredControls);
                        AddDiffSection(body, "Added Warnings", delta.AddedWarnings);
                        AddDiffSection(body, "Removed Warnings", delta.RemovedWarnings);
                        AddSpacer(body);
                    }
                }

                if (report.ManifestDiff is not null)
                {
                    AddHeading(body, "Manifest Diff", 2);
                    AddDiffSection(body, "Added Services", report.ManifestDiff.AddedServices);
                    AddDiffSection(body, "Removed Services", report.ManifestDiff.RemovedServices);
                    AddDiffSection(body, "Added Datastores", report.ManifestDiff.AddedDatastores);
                    AddDiffSection(body, "Removed Datastores", report.ManifestDiff.RemovedDatastores);
                    AddDiffSection(body, "Added Required Controls", report.ManifestDiff.AddedRequiredControls);
                    AddDiffSection(body, "Removed Required Controls", report.ManifestDiff.RemovedRequiredControls);
                    AddSpacer(body);
                }

                if (report.ExportDiffs.Count > 0)
                {
                    AddHeading(body, "Export Diffs", 2);
                    foreach (ExportRecordDiffResult diff in report.ExportDiffs)
                    {
                        AddParagraph(body, $"{diff.LeftExportRecordId} -> {diff.RightExportRecordId}", bold: true);
                        AddDiffSection(body, "Changed Top-Level Fields", diff.ChangedTopLevelFields);
                        AddDiffSection(body, "Changed Request Flags", diff.RequestDiff.ChangedFlags);
                        AddDiffSection(body, "Changed Request Values", diff.RequestDiff.ChangedValues);
                        AddDiffSection(body, "Warnings", diff.Warnings);
                        AddSpacer(body);
                    }
                }
            }

            AddHeading(body, "Interpretation Notes", 2);
            AddDiffSection(body, "Notes", report.InterpretationNotes);
            AddHeading(body, "Warnings", 2);
            AddDiffSection(body, "Warnings", report.Warnings);

            mainPart.Document.Save();
        }

        return Task.FromResult(stream.ToArray());
    }

    /// <summary>
    /// Renders <paramref name="report"/> as a PDF byte array using QuestPDF under the given export <paramref name="profile"/>.
    /// </summary>
    public Task<byte[]> GeneratePdfAsync(
        EndToEndReplayComparisonReport report,
        CancellationToken cancellationToken = default,
        string? profile = null)
    {
        ArgumentNullException.ThrowIfNull(report);
        cancellationToken.ThrowIfCancellationRequested();
        string p = EndToEndComparisonExportProfile.Normalize(profile);

        QuestPdfDocument doc = QuestPdfDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Helvetica"));

                page.Header().Text("ArchLucid End-to-End Replay Comparison").Bold().FontSize(14);
                page.Content().Column(column =>
                {
                    column.Item().PaddingBottom(5).Text($"Left: {report.LeftRunId}  |  Right: {report.RightRunId}  |  Profile: {p}");
                    column.Item().PaddingBottom(10).Text($"Generated: {DateTime.UtcNow:O}");
                    column.Item().PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    column.Item().PaddingBottom(5).Text("Summary").Bold().FontSize(12);
                    column.Item().PaddingBottom(10).Text(summaryFormatter.FormatMarkdown(report).Trim());

                    if (EndToEndComparisonExportProfile.IsShort(p))
                    {
                        column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        column.Item().PaddingTop(5).Text("Interpretation Notes").Bold();
                    }
                    else
                    {
                        column.Item().PaddingTop(5).Text("Key counts").Bold().FontSize(12);
                        column.Item().Text($"Run metadata: {report.RunDiff.ChangedFields.Count} changed field(s); Request IDs differ: {(report.RunDiff.RequestIdsDiffer ? "Yes" : "No")}");
                        if (report.AgentResultDiff is not null)
                        {
                            int withChanges = report.AgentResultDiff.AgentDeltas.Count(d =>
                                d.AddedClaims.Count > 0 || d.RemovedClaims.Count > 0 || d.AddedFindings.Count > 0 ||
                                d.RemovedFindings.Count > 0 || d.AddedRequiredControls.Count > 0 || d.RemovedRequiredControls.Count > 0 ||
                                d.AddedWarnings.Count > 0 || d.RemovedWarnings.Count > 0);
                            column.Item().Text($"Agent deltas: {withChanges} agent(s) with material changes");
                        }
                        if (report.ManifestDiff is not null)
                            column.Item().Text($"Manifest: +{report.ManifestDiff.AddedServices.Count} / -{report.ManifestDiff.RemovedServices.Count} services; +{report.ManifestDiff.AddedDatastores.Count} / -{report.ManifestDiff.RemovedDatastores.Count} datastores");
                        column.Item().Text($"Export diffs: {report.ExportDiffs.Count}");
                        column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        column.Item().PaddingTop(5).Text("Interpretation Notes").Bold();
                    }

                    foreach (string note in report.InterpretationNotes)
                        column.Item().Text($"• {note}");
                    column.Item().PaddingTop(5).Text("Warnings").Bold();
                    foreach (string w in report.Warnings)
                        column.Item().Text($"• {w}");
                });
            });
        });

        using MemoryStream stream = new();
        doc.GeneratePdf(stream);
        return Task.FromResult(stream.ToArray());
    }

    private static void AppendMarkdownHeader(StringBuilder sb, EndToEndReplayComparisonReport report)
    {
        sb.AppendLine("# ArchLucid End-to-End Replay Comparison Export");
        sb.AppendLine();
        sb.AppendLine($"- Left Run ID: {report.LeftRunId}");
        sb.AppendLine($"- Right Run ID: {report.RightRunId}");
        sb.AppendLine($"- Generated UTC: {DateTime.UtcNow:O}");
        sb.AppendLine();
    }

    private void AppendMarkdownExecutiveSummary(StringBuilder sb, EndToEndReplayComparisonReport report)
    {
        sb.AppendLine("## Key counts");
        sb.AppendLine();
        sb.AppendLine($"- Run metadata: {report.RunDiff.ChangedFields.Count} changed field(s); Request IDs differ: {(report.RunDiff.RequestIdsDiffer ? "Yes" : "No")}");
        if (report.AgentResultDiff is not null)
        {
            int withChanges = report.AgentResultDiff.AgentDeltas.Count(d =>
                d.AddedClaims.Count > 0 || d.RemovedClaims.Count > 0 || d.AddedFindings.Count > 0 ||
                d.RemovedFindings.Count > 0 || d.AddedRequiredControls.Count > 0 || d.RemovedRequiredControls.Count > 0 ||
                d.AddedWarnings.Count > 0 || d.RemovedWarnings.Count > 0);
            sb.AppendLine($"- Agent deltas: {withChanges} agent(s) with material changes");
        }
        if (report.ManifestDiff is not null)
            sb.AppendLine($"- Manifest: +{report.ManifestDiff.AddedServices.Count} / -{report.ManifestDiff.RemovedServices.Count} services; +{report.ManifestDiff.AddedDatastores.Count} / -{report.ManifestDiff.RemovedDatastores.Count} datastores");
        sb.AppendLine($"- Export diffs: {report.ExportDiffs.Count}");
        sb.AppendLine();
    }

    private static void AppendMarkdownRunMetadataDiff(StringBuilder sb, EndToEndReplayComparisonReport report)
    {
        sb.AppendLine("## Run Metadata Diff");
        sb.AppendLine();
        AppendList(sb, "Changed Fields", report.RunDiff.ChangedFields);
        sb.AppendLine($"- Request IDs Differ: {(report.RunDiff.RequestIdsDiffer ? "Yes" : "No")}");
        sb.AppendLine($"- Manifest Versions Differ: {(report.RunDiff.ManifestVersionsDiffer ? "Yes" : "No")}");
        sb.AppendLine($"- Status Differs: {(report.RunDiff.StatusDiffers ? "Yes" : "No")}");
        sb.AppendLine($"- Completion State Differs: {(report.RunDiff.CompletionStateDiffers ? "Yes" : "No")}");
        sb.AppendLine();
    }

    private static void AppendMarkdownAgentResultDiff(StringBuilder sb, EndToEndReplayComparisonReport report)
    {
        if (report.AgentResultDiff is null)
            return;
        sb.AppendLine("## Agent Result Diff");
        sb.AppendLine();
        foreach (AgentResultDelta delta in report.AgentResultDiff.AgentDeltas.OrderBy(x => x.AgentType))
        {
            sb.AppendLine($"### {delta.AgentType}");
            sb.AppendLine();
            sb.AppendLine($"- Left Exists: {(delta.LeftExists ? "Yes" : "No")}");
            sb.AppendLine($"- Right Exists: {(delta.RightExists ? "Yes" : "No")}");
            sb.AppendLine($"- Left Confidence: {(delta.LeftConfidence.HasValue ? delta.LeftConfidence.Value.ToString("0.00") : "n/a")}");
            sb.AppendLine($"- Right Confidence: {(delta.RightConfidence.HasValue ? delta.RightConfidence.Value.ToString("0.00") : "n/a")}");
            sb.AppendLine();
            AppendList(sb, "Added Claims", delta.AddedClaims);
            AppendList(sb, "Removed Claims", delta.RemovedClaims);
            AppendList(sb, "Added Findings", delta.AddedFindings);
            AppendList(sb, "Removed Findings", delta.RemovedFindings);
            AppendList(sb, "Added Required Controls", delta.AddedRequiredControls);
            AppendList(sb, "Removed Required Controls", delta.RemovedRequiredControls);
            AppendList(sb, "Added Warnings", delta.AddedWarnings);
            AppendList(sb, "Removed Warnings", delta.RemovedWarnings);
        }
    }

    private static void AppendMarkdownManifestDiff(StringBuilder sb, EndToEndReplayComparisonReport report)
    {
        if (report.ManifestDiff is null)
            return;
        sb.AppendLine("## Manifest Diff");
        sb.AppendLine();
        AppendList(sb, "Added Services", report.ManifestDiff.AddedServices);
        AppendList(sb, "Removed Services", report.ManifestDiff.RemovedServices);
        AppendList(sb, "Added Datastores", report.ManifestDiff.AddedDatastores);
        AppendList(sb, "Removed Datastores", report.ManifestDiff.RemovedDatastores);
        AppendList(sb, "Added Required Controls", report.ManifestDiff.AddedRequiredControls);
        AppendList(sb, "Removed Required Controls", report.ManifestDiff.RemovedRequiredControls);
        
        if (report.ManifestDiff.AddedRelationships.Count > 0)
        {
            sb.AppendLine("### Added Relationships");
            sb.AppendLine();
            foreach (RelationshipDiffItem rel in report.ManifestDiff.AddedRelationships)
                sb.AppendLine($"- {rel.SourceId} -> {rel.TargetId} ({rel.RelationshipType})");
            sb.AppendLine();
        }

        if (report.ManifestDiff.RemovedRelationships.Count <= 0) return;

        sb.AppendLine("### Removed Relationships");
        sb.AppendLine();
        foreach (RelationshipDiffItem rel in report.ManifestDiff.RemovedRelationships)
            sb.AppendLine($"- {rel.SourceId} -> {rel.TargetId} ({rel.RelationshipType})");
        sb.AppendLine();
    }

    private static void AppendMarkdownExportDiffs(StringBuilder sb, EndToEndReplayComparisonReport report)
    {
        if (report.ExportDiffs.Count == 0)
            return;
        sb.AppendLine("## Export Diffs");
        sb.AppendLine();
        foreach (ExportRecordDiffResult diff in report.ExportDiffs)
        {
            sb.AppendLine($"### {diff.LeftExportRecordId} -> {diff.RightExportRecordId}");
            sb.AppendLine();
            AppendList(sb, "Changed Top-Level Fields", diff.ChangedTopLevelFields);
            AppendList(sb, "Changed Request Flags", diff.RequestDiff.ChangedFlags);
            AppendList(sb, "Changed Request Values", diff.RequestDiff.ChangedValues);
            AppendList(sb, "Warnings", diff.Warnings);
        }
    }

    private static string EscapeHtml(string text) => WebUtility.HtmlEncode(text);

    private static string MarkdownToSimpleHtml(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return "";
        StringBuilder sb = new();
        foreach (string line in markdown.Split('\n'))
        {
            string t = line.TrimEnd();
            if (t.StartsWith("## "))
                sb.AppendLine("<h2>" + EscapeHtml(t[3..]) + "</h2>");
            else if (t.StartsWith("# "))
                sb.AppendLine("<h1>" + EscapeHtml(t[2..]) + "</h1>");
            else if (t.StartsWith("- "))
                sb.AppendLine("<li>" + EscapeHtml(t[2..]) + "</li>");
            else if (t.Length > 0)
                sb.AppendLine("<p>" + EscapeHtml(t) + "</p>");
            else
                sb.AppendLine("<br/>");
        }
        return sb.ToString();
    }

    private void AppendHtmlExecutiveSummary(StringBuilder sb, EndToEndReplayComparisonReport report)
    {
        sb.AppendLine("<h2>Key counts</h2><ul>");
        sb.AppendLine("<li>Run metadata: " + report.RunDiff.ChangedFields.Count + " changed field(s); Request IDs differ: " + (report.RunDiff.RequestIdsDiffer ? "Yes" : "No") + "</li>");
        if (report.AgentResultDiff is not null)
        {
            int withChanges = report.AgentResultDiff.AgentDeltas.Count(d =>
                d.AddedClaims.Count > 0 || d.RemovedClaims.Count > 0 || d.AddedFindings.Count > 0 ||
                d.RemovedFindings.Count > 0 || d.AddedRequiredControls.Count > 0 || d.RemovedRequiredControls.Count > 0 ||
                d.AddedWarnings.Count > 0 || d.RemovedWarnings.Count > 0);
            sb.AppendLine("<li>Agent deltas: " + withChanges + " agent(s) with material changes</li>");
        }
        if (report.ManifestDiff is not null)
            sb.AppendLine("<li>Manifest: +" + report.ManifestDiff.AddedServices.Count + " / -" + report.ManifestDiff.RemovedServices.Count + " services; +" + report.ManifestDiff.AddedDatastores.Count + " / -" + report.ManifestDiff.RemovedDatastores.Count + " datastores</li>");
        sb.AppendLine("<li>Export diffs: " + report.ExportDiffs.Count + "</li></ul>");
    }

    private static void AppendHtmlRunMetadataDiff(StringBuilder sb, EndToEndReplayComparisonReport report)
    {
        sb.AppendLine("<h2>Run Metadata Diff</h2><ul>");
        sb.AppendLine("<li>Request IDs Differ: " + (report.RunDiff.RequestIdsDiffer ? "Yes" : "No") + "</li>");
        sb.AppendLine("<li>Manifest Versions Differ: " + (report.RunDiff.ManifestVersionsDiffer ? "Yes" : "No") + "</li>");
        sb.AppendLine("<li>Status Differs: " + (report.RunDiff.StatusDiffers ? "Yes" : "No") + "</li>");
        sb.AppendLine("<li>Completion State Differs: " + (report.RunDiff.CompletionStateDiffers ? "Yes" : "No") + "</li>");
        foreach (string f in report.RunDiff.ChangedFields)
            sb.AppendLine("<li>Changed field: " + EscapeHtml(f) + "</li>");
        sb.AppendLine("</ul>");
    }

    private static void AppendHtmlAgentResultDiff(StringBuilder sb, EndToEndReplayComparisonReport report)
    {
        if (report.AgentResultDiff is null)
            return;
        sb.AppendLine("<h2>Agent Result Diff</h2>");
        foreach (AgentResultDelta delta in report.AgentResultDiff.AgentDeltas.OrderBy(x => x.AgentType))
        {
            sb.AppendLine("<h3>" + EscapeHtml(delta.AgentType.ToString()) + "</h3><ul>");
            sb.AppendLine("<li>Left Exists: " + (delta.LeftExists ? "Yes" : "No") + "</li>");
            sb.AppendLine("<li>Right Exists: " + (delta.RightExists ? "Yes" : "No") + "</li>");
            foreach (string c in delta.AddedClaims)
                sb.AppendLine("<li>Added claim: " + EscapeHtml(c) + "</li>");
            foreach (string c in delta.RemovedClaims)
                sb.AppendLine("<li>Removed claim: " + EscapeHtml(c) + "</li>");
            foreach (string f in delta.AddedFindings)
                sb.AppendLine("<li>Added finding: " + EscapeHtml(f) + "</li>");
            foreach (string f in delta.RemovedFindings)
                sb.AppendLine("<li>Removed finding: " + EscapeHtml(f) + "</li>");
            sb.AppendLine("</ul>");
        }
    }

    private static void AppendHtmlManifestDiff(StringBuilder sb, EndToEndReplayComparisonReport report)
    {
        if (report.ManifestDiff is null)
            return;
        sb.AppendLine("<h2>Manifest Diff</h2><ul>");
        foreach (string s in report.ManifestDiff.AddedServices)
            sb.AppendLine("<li>Added service: " + EscapeHtml(s) + "</li>");
        foreach (string s in report.ManifestDiff.RemovedServices)
            sb.AppendLine("<li>Removed service: " + EscapeHtml(s) + "</li>");
        foreach (string d in report.ManifestDiff.AddedDatastores)
            sb.AppendLine("<li>Added datastore: " + EscapeHtml(d) + "</li>");
        foreach (string d in report.ManifestDiff.RemovedDatastores)
            sb.AppendLine("<li>Removed datastore: " + EscapeHtml(d) + "</li>");
        sb.AppendLine("</ul>");
    }

    private static void AppendHtmlExportDiffs(StringBuilder sb, EndToEndReplayComparisonReport report)
    {
        if (report.ExportDiffs.Count == 0)
            return;
        sb.AppendLine("<h2>Export Diffs</h2>");
        foreach (ExportRecordDiffResult diff in report.ExportDiffs)
        {
            sb.AppendLine("<h3>" + EscapeHtml(diff.LeftExportRecordId + " -> " + diff.RightExportRecordId) + "</h3><ul>");
            foreach (string f in diff.ChangedTopLevelFields)
                sb.AppendLine("<li>" + EscapeHtml(f) + "</li>");
            foreach (string w in diff.Warnings)
                sb.AppendLine("<li>Warning: " + EscapeHtml(w) + "</li>");
            sb.AppendLine("</ul>");
        }
    }

    private static void AppendHtmlList(StringBuilder sb, string title, IReadOnlyCollection<string> items)
    {
        sb.AppendLine("<h2>" + EscapeHtml(title) + "</h2><ul>");
        if (items.Count == 0)
            sb.AppendLine("<li>None</li>");
        else
            foreach (string item in items)
                sb.AppendLine("<li>" + EscapeHtml(item) + "</li>");
        sb.AppendLine("</ul>");
    }

    private void AddDocxExecutiveSummary(Body body, EndToEndReplayComparisonReport report)
    {
        AddHeading(body, "Key counts", 2);
        AddBullet(body, $"Run metadata: {report.RunDiff.ChangedFields.Count} changed field(s); Request IDs differ: {(report.RunDiff.RequestIdsDiffer ? "Yes" : "No")}");
        if (report.AgentResultDiff is not null)
        {
            int withChanges = report.AgentResultDiff.AgentDeltas.Count(d =>
                d.AddedClaims.Count > 0 || d.RemovedClaims.Count > 0 || d.AddedFindings.Count > 0 ||
                d.RemovedFindings.Count > 0 || d.AddedRequiredControls.Count > 0 || d.RemovedRequiredControls.Count > 0 ||
                d.AddedWarnings.Count > 0 || d.RemovedWarnings.Count > 0);
            AddBullet(body, $"Agent deltas: {withChanges} agent(s) with material changes");
        }
        if (report.ManifestDiff is not null)
            AddBullet(body, $"Manifest: +{report.ManifestDiff.AddedServices.Count} / -{report.ManifestDiff.RemovedServices.Count} services; +{report.ManifestDiff.AddedDatastores.Count} / -{report.ManifestDiff.RemovedDatastores.Count} datastores");
        AddBullet(body, $"Export diffs: {report.ExportDiffs.Count}");
        AddSpacer(body);
    }

    private static void AppendList(StringBuilder sb, string title, IReadOnlyCollection<string> items)
    {
        sb.AppendLine($"### {title}");
        sb.AppendLine();

        if (items.Count == 0)
        {
            sb.AppendLine("- None");
            sb.AppendLine();
            return;
        }

        foreach (string item in items)
        
            sb.AppendLine($"- {item}");
        

        sb.AppendLine();
    }

    private static void AddHeading(Body body, string text, int level)
    {
        body.AppendChild(new Paragraph(
            new ParagraphProperties(
                new ParagraphStyleId { Val = $"Heading{level}" }),
            new Run(new Text(text))));
    }

    private static void AddParagraph(Body body, string text, bool bold = false)
    {
        Run run = new(new Text(text) { Space = SpaceProcessingModeValues.Preserve });

        if (bold)
        
            run.RunProperties = new RunProperties(new Bold());
        

        body.AppendChild(new Paragraph(run));
    }

    private static void AddBullet(Body body, string text)
    {
        body.AppendChild(new Paragraph(
            new Run(new Text($"• {text}") { Space = SpaceProcessingModeValues.Preserve })));
    }

    private static void AddSpacer(Body body)
    {
        body.AppendChild(new Paragraph(new Run(new Text(string.Empty))));
    }

    private static void AddDiffSection(Body body, string title, IReadOnlyCollection<string> items)
    {
        AddParagraph(body, title, bold: true);

        if (items.Count == 0)
        {
            AddBullet(body, "None");
            return;
        }

        foreach (string item in items)
        
            AddBullet(body, item);
        
    }
}

