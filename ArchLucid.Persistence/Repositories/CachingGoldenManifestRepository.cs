using System.Data;

using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using ArchiForge.Persistence.Caching;

namespace ArchiForge.Persistence.Repositories;

/// <summary>
/// Decorates <see cref="IGoldenManifestRepository"/> with scoped hot-path reads and evicts on <see cref="SaveAsync"/>.
/// </summary>
public sealed class CachingGoldenManifestRepository(
    IGoldenManifestRepository inner,
    IHotPathReadCache hotPathReadCache) : IGoldenManifestRepository
{
    private readonly IGoldenManifestRepository _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    private readonly IHotPathReadCache _hotPathReadCache =
        hotPathReadCache ?? throw new ArgumentNullException(nameof(hotPathReadCache));

    /// <inheritdoc />
    public async Task SaveAsync(
        GoldenManifest manifest,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        await _inner.SaveAsync(manifest, ct, connection, transaction);

        await _hotPathReadCache.RemoveAsync(HotPathCacheKeys.Manifest(AmbientScope(manifest), manifest.ManifestId), ct);
    }

    /// <inheritdoc />
    public Task<GoldenManifest?> GetByIdAsync(ScopeContext scope, Guid manifestId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(scope);

        string key = HotPathCacheKeys.Manifest(scope, manifestId);

        return _hotPathReadCache.GetOrCreateAsync(
            key,
            innerCt => _inner.GetByIdAsync(scope, manifestId, innerCt),
            ct);
    }

    private static ScopeContext AmbientScope(GoldenManifest manifest) => new()
    {
        TenantId = manifest.TenantId,
        WorkspaceId = manifest.WorkspaceId,
        ProjectId = manifest.ProjectId
    };
}
