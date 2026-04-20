using ArchLucid.Contracts.Manifest;
using ArchLucid.Core.Scoping;

namespace ArchLucid.Decisioning.Interfaces;

/// <summary>
/// Read-only façade over coordinator and authority golden-manifest persistence (ADR 0021 Phase 1).
/// </summary>
public interface IUnifiedGoldenManifestReader
{
    /// <summary>Resolves a manifest by its immutable version key (coordinator table today).</summary>
    Task<GoldenManifest?> GetByVersionAsync(string manifestVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the best committed manifest for a run: prefers authority <see cref="IGoldenManifestRepository"/> when
    /// <c>GoldenManifestId</c> is set; otherwise falls back to coordinator <c>CurrentManifestVersion</c>.
    /// </summary>
    Task<GoldenManifest?> ReadByRunIdAsync(ScopeContext scope, Guid runId, CancellationToken cancellationToken = default);
}
