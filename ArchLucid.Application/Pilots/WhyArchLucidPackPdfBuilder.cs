using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using QuestPdfDocument = QuestPDF.Fluent.Document;

namespace ArchLucid.Application.Pilots;

/// <summary>
///     PDF projection for <see cref="WhyArchLucidPackBuilder" /> Markdown — same QuestPDF +
///     <see cref="MarkdownPdfRenderer" />
///     stack as <see cref="FirstValueReportPdfBuilder" />.
/// </summary>
public sealed class WhyArchLucidPackPdfBuilder
{
    /// <summary>Serializes <paramref name="markdown" /> to PDF bytes (single flowing document).</summary>
    public byte[] Build(string markdown)
    {
        if (markdown is null)
            throw new ArgumentNullException(nameof(markdown));

        Settings.License = LicenseType.Community;

        QuestPdfDocument doc = QuestPdfDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Helvetica"));

                page.Header()
                    .Background(Colors.Amber.Lighten4)
                    .Padding(6)
                    .Text("demo tenant — replace before publishing")
                    .Bold()
                    .FontSize(10);

                page.Content().Column(column => MarkdownPdfRenderer.Render(column, markdown));

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("ArchLucid — why-archlucid-pack.pdf — deterministic demo bundle");
                    });
            });
        });

        using MemoryStream stream = new();
        doc.GeneratePdf(stream);

        return stream.ToArray();
    }
}
