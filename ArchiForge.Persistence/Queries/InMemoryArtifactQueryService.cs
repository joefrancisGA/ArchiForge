using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.ArtifactSynthesis.Packaging;
using ArchiForge.Core.Scoping;

namespace ArchiForge.Persistence.Queries;

/// <summary>
/// Same behavior as <see cref="DapperArtifactQueryService"/>; type name reflects typical registration in storage-off mode.
/// </summary>
public sealed class InMemoryArtifactQueryService(IArtifactBundleRepository artifactBundleRepository)
    : IArtifactQueryService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ArtifactDescriptor>> ListArtifactsByManifestIdAsync(
        ScopeContext scope,
        Guid manifestId,
        CancellationToken ct)
    {
        IReadOnlyList<SynthesizedArtifact> artifacts = await GetArtifactsByManifestIdAsync(scope, manifestId, ct);
        return ArtifactDescriptorMapper.ToDescriptorList(artifacts);
    }

    /// <inheritdoc />
    public async Task<SynthesizedArtifact?> GetArtifactByIdAsync(
        ScopeContext scope,
        Guid manifestId,
        Guid artifactId,
        CancellationToken ct)
    {
        IReadOnlyList<SynthesizedArtifact> artifacts = await GetArtifactsByManifestIdAsync(scope, manifestId, ct);
        return artifacts.FirstOrDefault(x => x.ArtifactId == artifactId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SynthesizedArtifact>> GetArtifactsByManifestIdAsync(
        ScopeContext scope,
        Guid manifestId,
        CancellationToken ct)
    {
        ArtifactBundle? bundle = await artifactBundleRepository.GetByManifestIdAsync(scope, manifestId, ct);
        IReadOnlyList<SynthesizedArtifact> raw = bundle?.Artifacts ?? [];

        return ArtifactDescriptorMapper.OrderSynthesizedArtifacts(raw);
    }
}
