using ArchLucid.Core.Diagrams;

namespace ArchLucid.Application.Analysis;

/// <summary>
///     Generates consulting-grade DOCX reports from an <see cref="ArchitectureAnalysisReport" />
///     using a profile-driven template via <see cref="ConsultingDocxOpenXmlComposer" />.
/// </summary>
public sealed class ConsultingDocxArchitectureAnalysisExportService(
    IDiagramImageRenderer diagramImageRenderer,
    IConsultingDocxTemplateOptionsProvider optionsProvider,
    IDocumentLogoProvider logoProvider)
    : IArchitectureAnalysisConsultingDocxExportService
{
    public async Task<byte[]> GenerateDocxAsync(
        ArchitectureAnalysisReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        ConsultingDocxTemplateOptions options = optionsProvider.GetOptions();

        return await ConsultingDocxOpenXmlComposer.GenerateAsync(
            report,
            options,
            diagramImageRenderer,
            logoProvider,
            cancellationToken);
    }
}
