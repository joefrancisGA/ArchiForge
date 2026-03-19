using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Repositories;

public class InMemoryGoldenManifestRepository : IGoldenManifestRepository
{
    private readonly List<GoldenManifest> _store = [];

    public Task SaveAsync(GoldenManifest manifest, CancellationToken ct)
    {
        _store.Add(manifest);
        return Task.CompletedTask;
    }

    public Task<GoldenManifest?> GetByIdAsync(Guid manifestId, CancellationToken ct)
    {
        var result = _store.FirstOrDefault(x => x.ManifestId == manifestId);
        return Task.FromResult(result);
    }
}

