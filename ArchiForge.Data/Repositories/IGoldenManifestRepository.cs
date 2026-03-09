using ArchiForge.Contracts.Manifest;

namespace ArchiForge.Data.Repositories;

public interface IGoldenManifestRepository
{
    Task CreateAsync(GoldenManifest manifest, CancellationToken cancellationToken = default);
    Task<GoldenManifest?> GetByVersionAsync(string manifestVersion, CancellationToken cancellationToken = default);
}