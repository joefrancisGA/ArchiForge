using ArchiForge.Contracts.Manifest;

namespace ArchiForge.Application.Diffs;

public interface IManifestDiffExportService
{
    string GenerateMarkdownExport(
        GoldenManifest left,
        GoldenManifest right,
        ManifestDiffResult diff,
        string markdownSummary);
}
