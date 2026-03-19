using System.Linq;
using ArchiForge.Application.Diagrams;

namespace ArchiForge.Application.Analysis;

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

        var options = optionsProvider.GetOptions();

        return await ConsultingDocxOpenXmlComposer.GenerateAsync(
            report,
            options,
            diagramImageRenderer,
            logoProvider,
            cancellationToken);
    }
}

