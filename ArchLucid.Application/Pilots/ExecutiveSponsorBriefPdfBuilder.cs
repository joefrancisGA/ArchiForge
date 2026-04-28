using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using QuestPdfDocument = QuestPDF.Fluent.Document;

namespace ArchLucid.Application.Pilots;

/// <summary>
/// PDF projection of <c>docs/EXECUTIVE_SPONSOR_BRIEF.md</c> — same QuestPDF stack as <see cref="FirstValueReportPdfBuilder"/>;
/// does not mutate the Markdown file (read-only IO at controller).
/// </summary>
public sealed class ExecutiveSponsorBriefPdfBuilder
{
    /// <summary>Serializes sponsor-brief Markdown to PDF bytes.</summary>
    public byte[] Build(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        QuestPDF.Settings.License = LicenseType.Community;

        QuestPdfDocument doc = QuestPdfDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Helvetica"));

                page.Header().Text("ArchLucid — Executive Sponsor Brief").Bold().FontSize(14);

                page.Content().Column(column => MarkdownPdfRenderer.Render(column, markdown));

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                        text.Span("Canonical body: docs/EXECUTIVE_SPONSOR_BRIEF.md — PDF is a portable rendering"));
            });
        });

        using MemoryStream stream = new();
        doc.GeneratePdf(stream);

        return stream.ToArray();
    }
}
