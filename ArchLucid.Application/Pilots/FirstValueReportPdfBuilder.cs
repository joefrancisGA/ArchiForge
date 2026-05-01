using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using QuestPdfDocument = QuestPDF.Fluent.Document;

namespace ArchLucid.Application.Pilots;

/// <summary>
///     PDF projection of the canonical first-value-report Markdown produced by <see cref="FirstValueReportBuilder" />.
///     One sponsor-shareable PDF per committed run; the Markdown body remains the source of truth so PDF output
///     cannot drift from the existing <c>GET /v1/pilots/runs/{runId}/first-value-report</c> response.
/// </summary>
public sealed class FirstValueReportPdfBuilder(FirstValueReportBuilder markdownBuilder)
{
    private readonly FirstValueReportBuilder _markdownBuilder =
        markdownBuilder ?? throw new ArgumentNullException(nameof(markdownBuilder));

    /// <summary>Returns PDF bytes, or <see langword="null" /> when the run is missing (mirrors the Markdown sibling).</summary>
    public async Task<byte[]?> BuildPdfAsync(
        string runId,
        string apiBaseForLinks,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("Run id is required.", nameof(runId));

        string? markdown = await _markdownBuilder.BuildMarkdownAsync(runId, apiBaseForLinks, cancellationToken);

        if (markdown is null)
            return null;

        Settings.License = LicenseType.Community;

        QuestPdfDocument doc = QuestPdfDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Helvetica"));

                page.Header().Text("ArchLucid — first value report (pilot)").Bold().FontSize(14);
                page.Content().Column(column => MarkdownPdfRenderer.Render(column, markdown));
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generated from run ");
                    text.Span(runId).Bold();
                });
            });
        });

        using MemoryStream stream = new();
        doc.GeneratePdf(stream);

        return stream.ToArray();
    }
}
