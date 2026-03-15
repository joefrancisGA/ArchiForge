using ArchiForge.Contracts.Manifest;

namespace ArchiForge.Application.Diffs;

public interface IManifestDiffService
{
    ManifestDiffResult Compare(
        GoldenManifest left,
        GoldenManifest right);
}
