using System.Text.Json;

using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Manifest;

namespace ArchiForge.Persistence.Data.Repositories;

/// <summary>
/// Thread-safe in-memory <see cref="ICoordinatorGoldenManifestRepository"/> for coordinator/replay flows (JSON clone-on-read).
/// Distinct from the authority-layer in-memory manifest store registered in API storage extensions.
/// </summary>
public sealed class InMemoryCoordinatorGoldenManifestRepository : ICoordinatorGoldenManifestRepository
{
    private readonly Dictionary<string, GoldenManifest> _byVersion = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public Task CreateAsync(GoldenManifest manifest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(manifest.Metadata.ManifestVersion))
        
            throw new ArgumentException("Metadata.ManifestVersion is required.", nameof(manifest));
        

        lock (_gate)
        
            _byVersion[manifest.Metadata.ManifestVersion] = Clone(manifest);
        

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<GoldenManifest?> GetByVersionAsync(
        string manifestVersion,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        
            return Task.FromResult(
                _byVersion.TryGetValue(manifestVersion, out GoldenManifest? m) ? Clone(m) : null);
        
    }

    private static GoldenManifest Clone(GoldenManifest source)
    {
        string json = JsonSerializer.Serialize(source, ContractJson.Default);
        GoldenManifest? copy = JsonSerializer.Deserialize<GoldenManifest>(json, ContractJson.Default);

        return copy ?? throw new InvalidOperationException("Clone produced null GoldenManifest.");
    }
}
