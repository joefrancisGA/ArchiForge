using ArchiForge.Contracts.Manifest;

namespace ArchiForge.Persistence.Data.Repositories;

/// <summary>
/// Persistence contract for <see cref="GoldenManifest"/> records.
/// A manifest is immutable once created; use a new version string to publish an updated manifest.
/// </summary>
public interface ICoordinatorGoldenManifestRepository
{
    /// <summary>
    /// Persists a new manifest snapshot.
    /// <paramref name="manifest"/> must have a non-empty <c>Metadata.ManifestVersion</c>.
    /// Implementors should not overwrite an existing version; callers are expected to generate unique version strings.
    /// </summary>
    Task CreateAsync(GoldenManifest manifest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the manifest with the specified <paramref name="manifestVersion"/>, or <see langword="null"/> when not found.
    /// </summary>
    Task<GoldenManifest?> GetByVersionAsync(string manifestVersion, CancellationToken cancellationToken = default);
}
