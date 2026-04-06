using System.Data;

using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Repositories;

/// <summary>
/// In-memory implementation of <see cref="IGoldenManifestRepository"/> for testing and local development.
/// Capped at 500 entries; oldest entries are evicted when the cap is exceeded.
/// </summary>
public class InMemoryGoldenManifestRepository : IGoldenManifestRepository
{
    private const int MaxEntries = 500;

    private readonly List<GoldenManifest> _store = [];
    private readonly Lock _lock = new();

    public Task SaveAsync(
        GoldenManifest manifest,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ct.ThrowIfCancellationRequested();
        _ = connection;
        _ = transaction;
        lock (_lock)
        {
            _store.Add(manifest);
            if (_store.Count > MaxEntries)
                _store.RemoveRange(0, _store.Count - MaxEntries);
        }
        return Task.CompletedTask;
    }

    public Task<GoldenManifest?> GetByIdAsync(ScopeContext scope, Guid manifestId, CancellationToken ct)
    {
        lock (_lock)
        {
            GoldenManifest? result = _store.FirstOrDefault(x =>
                x.ManifestId == manifestId &&
                x.TenantId == scope.TenantId &&
                x.WorkspaceId == scope.WorkspaceId &&
                x.ProjectId == scope.ProjectId);
            return Task.FromResult(result);
        }
    }
}

