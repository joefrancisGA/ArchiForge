using ArchLucid.ArtifactSynthesis.Interfaces;
using ArchLucid.ArtifactSynthesis.Models;
using ArchLucid.ArtifactSynthesis.Packaging;
using ArchLucid.Core.Scoping;

namespace ArchLucid.Persistence.Queries;

/// <summary>
///     <see cref="IArtifactQueryService" /> backed by
///     <see cref="ArchLucid.ArtifactSynthesis.Interfaces.IArtifactBundleRepository" /> (SQL or in-memory depending on DI).
/// </summary>
public sealed class DapperArtifactQueryService(IArtifactBundleRepository artifactBundleRepository)
    : IArtifactQueryService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ArtifactDescriptor>> ListArtifactsByManifestIdAsync(
        ScopeContext scope,
        Guid manifestId,
        CancellationToken ct)
    {
        ArtifactBundle? bundle = await artifactBundleRepository.GetByManifestIdAsync(scope, manifestId,
            loadArtifactBodies: false, ct);
        IReadOnlyList<SynthesizedArtifact> raw = bundle?.Artifacts ?? [];

        IReadOnlyList<SynthesizedArtifact> sorted = ArtifactDescriptorMapper.OrderSynthesizedArtifacts(raw);
        return ArtifactDescriptorMapper.ToDescriptorList(sorted);
    }

    /// <inheritdoc />
    public async Task<SynthesizedArtifact?> GetArtifactByIdAsync(
        ScopeContext scope,
        Guid manifestId,
        Guid artifactId,
        CancellationToken ct)
    {
        ArtifactBundle? bundle = await artifactBundleRepository.GetByManifestIdAsync(scope, manifestId,
            loadArtifactBodies: true, ct);

        return bundle?.Artifacts.FirstOrDefault(x => x.ArtifactId == artifactId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SynthesizedArtifact>> GetArtifactsByManifestIdAsync(
        ScopeContext scope,
        Guid manifestId,
        CancellationToken ct)
    {
        ArtifactBundle? bundle = await artifactBundleRepository.GetByManifestIdAsync(scope, manifestId,
            loadArtifactBodies: true, ct);
        IReadOnlyList<SynthesizedArtifact> raw = bundle?.Artifacts ?? [];

        return ArtifactDescriptorMapper.OrderSynthesizedArtifacts(raw);
    }
}
