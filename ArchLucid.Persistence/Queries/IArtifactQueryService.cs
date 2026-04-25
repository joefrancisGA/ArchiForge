using ArchLucid.ArtifactSynthesis.Models;
using ArchLucid.ArtifactSynthesis.Packaging;
using ArchLucid.Core.Scoping;

namespace ArchLucid.Persistence.Queries;

/// <summary>
///     Read-only access to synthesized artifacts for a manifest via the artifact bundle repository.
/// </summary>
/// <remarks>
///     SQL: <see cref="DapperArtifactQueryService" />; in-memory: <see cref="InMemoryArtifactQueryService" />.
///     Callers: <c>ArchLucid.Api.Controllers.DocxExportController</c>, <c>ArtifactExportController</c>.
/// </remarks>
public interface IArtifactQueryService
{
    /// <summary>Lightweight descriptors sorted by name then artifact id (no full content load beyond bundle read).</summary>
    /// <param name="scope">Caller scope for tenant/workspace/project isolation.</param>
    /// <param name="manifestId">Golden manifest whose artifacts to list.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Descriptor list (may be empty when no artifact bundle exists).</returns>
    Task<IReadOnlyList<ArtifactDescriptor>> ListArtifactsByManifestIdAsync(
        ScopeContext scope,
        Guid manifestId,
        CancellationToken ct);

    /// <summary>Single artifact body when present in the bundle for <paramref name="manifestId" />.</summary>
    /// <param name="scope">Caller scope for tenant/workspace/project isolation.</param>
    /// <param name="manifestId">Golden manifest id.</param>
    /// <param name="artifactId">Artifact id within the bundle.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Full artifact, or <see langword="null" /> when not found in the bundle.</returns>
    Task<SynthesizedArtifact?> GetArtifactByIdAsync(
        ScopeContext scope,
        Guid manifestId,
        Guid artifactId,
        CancellationToken ct);

    /// <summary>All artifacts in the bundle for the manifest, or empty when no bundle.</summary>
    /// <param name="scope">Caller scope for tenant/workspace/project isolation.</param>
    /// <param name="manifestId">Golden manifest id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Full artifact list (may be empty); same name/type ordering as list descriptors (name, then id).</returns>
    Task<IReadOnlyList<SynthesizedArtifact>> GetArtifactsByManifestIdAsync(
        ScopeContext scope,
        Guid manifestId,
        CancellationToken ct);
}
