using ArchiForge.ArtifactSynthesis.Models;

namespace ArchiForge.ArtifactSynthesis.Interfaces;

public interface IArtifactBundleRepository
{
    Task SaveAsync(ArtifactBundle bundle, CancellationToken ct);
    Task<ArtifactBundle?> GetByManifestIdAsync(Guid manifestId, CancellationToken ct);
}
