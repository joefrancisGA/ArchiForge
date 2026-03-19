using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Models;

namespace ArchiForge.ArtifactSynthesis.Repositories;

public class InMemoryArtifactBundleRepository : IArtifactBundleRepository
{
    private readonly List<ArtifactBundle> _store = new();

    public Task SaveAsync(ArtifactBundle bundle, CancellationToken ct)
    {
        _store.Add(bundle);
        return Task.CompletedTask;
    }

    public Task<ArtifactBundle?> GetByManifestIdAsync(Guid manifestId, CancellationToken ct)
    {
        var result = _store.LastOrDefault(x => x.ManifestId == manifestId);
        return Task.FromResult(result);
    }
}
