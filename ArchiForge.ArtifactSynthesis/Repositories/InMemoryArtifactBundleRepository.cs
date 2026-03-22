using System.Data;
using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.Core.Scoping;
using ArchiForge.ArtifactSynthesis.Models;

namespace ArchiForge.ArtifactSynthesis.Repositories;

public class InMemoryArtifactBundleRepository : IArtifactBundleRepository
{
    private readonly List<ArtifactBundle> _store = [];

    public Task SaveAsync(
        ArtifactBundle bundle,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        _ = ct;
        _ = connection;
        _ = transaction;
        _store.Add(bundle);
        return Task.CompletedTask;
    }

    public Task<ArtifactBundle?> GetByManifestIdAsync(ScopeContext scope, Guid manifestId, CancellationToken ct)
    {
        var result = _store.LastOrDefault(x =>
            x.ManifestId == manifestId &&
            x.TenantId == scope.TenantId &&
            x.WorkspaceId == scope.WorkspaceId &&
            x.ProjectId == scope.ProjectId);
        return Task.FromResult(result);
    }
}
