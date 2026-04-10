using ArchLucid.ArtifactSynthesis.Models;

namespace ArchLucid.ArtifactSynthesis.Packaging;

public interface IArtifactPackagingService
{
    ArtifactFileExport BuildSingleFileExport(SynthesizedArtifact artifact);

    ArtifactPackage BuildBundlePackage(
        Guid manifestId,
        IReadOnlyList<SynthesizedArtifact> artifacts);

    ArtifactPackage BuildRunExportPackage(
        Guid runId,
        Guid manifestId,
        IReadOnlyList<SynthesizedArtifact> artifacts,
        string manifestJson,
        string? traceJson = null,
        RunExportReadmeContext? readmeContext = null);
}
