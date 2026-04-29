using System.Data;

using ArchLucid.ArtifactSynthesis.Models;
using ArchLucid.Core.Scoping;

namespace ArchLucid.ArtifactSynthesis.Interfaces;

/// <summary>
///     Persistence contract for <see cref="ArtifactBundle" /> records that group the synthesized
///     output artifacts (diagrams, documents, reports) produced for a golden manifest.
/// </summary>
public interface IArtifactBundleRepository
{
    /// <summary>
    ///     Persists an artifact bundle. Callers may pass an existing <paramref name="connection" />
    ///     and <paramref name="transaction" /> to participate in a multi-statement transaction.
    /// </summary>
    /// <param name="bundle">The bundle to persist.</param>
    /// <param name="ct">Propagates notification that the operation should be cancelled.</param>
    /// <param name="connection">Optional open connection to reuse.</param>
    /// <param name="transaction">Optional transaction to enlist in.</param>
    Task SaveAsync(
        ArtifactBundle bundle,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);

    /// <summary>
    ///     Returns the artifact bundle associated with <paramref name="manifestId" /> within
    ///     <paramref name="scope" />, or <see langword="null" /> when none exists or the manifest
    ///     is outside the caller's scope.
    /// </summary>
    /// <param name="scope">Tenant/workspace/project boundary enforced by the implementation.</param>
    /// <param name="manifestId">The golden manifest whose bundle is requested.</param>
    /// <param name="loadArtifactBodies">
    ///     When <see langword="false" />, relational reads omit artifact <c>Content</c> payloads (descriptor/list paths).
    ///     Export and download flows pass <see langword="true" />.
    /// </param>
    /// <param name="ct">Propagates notification that the operation should be cancelled.</param>
    Task<ArtifactBundle?> GetByManifestIdAsync(
        ScopeContext scope,
        Guid manifestId,
        bool loadArtifactBodies,
        CancellationToken ct);
}
