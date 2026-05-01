using ArchLucid.Core.Diagrams;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ArchLucid.Application.Analysis;

/// <summary>
///     Consolidates all OpenXML usage for the consulting DOCX export into one place.
///     Export services should depend on this composer instead of manipulating OpenXML types directly.
/// </summary>
internal static class ConsultingDocxOpenXmlComposer
{
    public static async Task<byte[]> GenerateAsync(
        ArchitectureAnalysisReport report,
        ConsultingDocxTemplateOptions options,
        IDiagramImageRenderer diagramImageRenderer,
        IDocumentLogoProvider logoProvider,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(diagramImageRenderer);
        ArgumentNullException.ThrowIfNull(logoProvider);

        using MemoryStream stream = new();

        using (WordprocessingDocument document = WordprocessingDocument.Create(
                   stream,
                   WordprocessingDocumentType.Document,
                   true))
        {
            MainDocumentPart mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
            Body body = mainPart.Document.Body!;

            ConsultingDocxOpenXmlPrimitives.AddStylesPart(mainPart, options);

            await ConsultingDocxCoverPageBuilder.AddAsync(mainPart, body, report, options, logoProvider,
                cancellationToken);
            ConsultingDocxOpenXmlPrimitives.AddPageBreak(body);

            if (options.IncludeDocumentControl)
            {
                ConsultingDocxSupplementalSections.AddDocumentControl(body, report);
                ConsultingDocxOpenXmlPrimitives.AddPageBreak(body);
            }

            if (options.IncludeTableOfContents)
            {
                ConsultingDocxSupplementalSections.AddTableOfContentsPlaceholder(body);
                ConsultingDocxOpenXmlPrimitives.AddPageBreak(body);
            }

            if (options.IncludeExecutiveSummary)

                ConsultingDocxSupplementalSections.AddExecutiveSummary(body, report, options);

            if (options.IncludeArchitectureOverview)

                await ConsultingDocxSupplementalSections.AddArchitectureOverviewAsync(
                    body,
                    mainPart,
                    report,
                    options,
                    diagramImageRenderer,
                    cancellationToken);

            if (options.IncludeEvidenceAndConstraints)

                ConsultingDocxFindingsSectionBuilder.Add(body, report);

            if (options.IncludeArchitectureDetails)

                ConsultingDocxSupplementalSections.AddArchitectureDetails(body, report);

            if (options.IncludeGovernanceAndControls)

                ConsultingDocxSupplementalSections.AddGovernanceAndControls(body, report);

            if (options.IncludeExplainabilitySection)

                ConsultingDocxSupplementalSections.AddExplainabilitySection(body, report, options);

            if (options.IncludeConclusions)

                ConsultingDocxRecommendationsSectionBuilder.Add(body, report, options);

            ConsultingDocxSupplementalSections.AddAppendices(body, report, options);

            mainPart.Document.Save();
        }

        return stream.ToArray();
    }
}
