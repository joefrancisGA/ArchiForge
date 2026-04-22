using System.Data;

using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Manifest.Mapping;
using ArchLucid.Decisioning.Models;
using Cm = ArchLucid.Contracts.Manifest;

namespace ArchLucid.Decisioning.Repositories;

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

    /// <inheritdoc />
    public Task<GoldenManifest> SaveAsync(
        Cm.GoldenManifest contract,
        ScopeContext scope,
        SaveContractsManifestOptions keying,
        IManifestHashService manifestHashService,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null,
        GoldenManifest? authorityPersistBody = null)
    {
        if (contract is null)
            throw new ArgumentNullException(nameof(contract));
        if (scope is null)
            throw new ArgumentNullException(nameof(scope));
        if (keying is null)
            throw new ArgumentNullException(nameof(keying));
        if (manifestHashService is null)
            throw new ArgumentNullException(nameof(manifestHashService));
        _ = connection;
        _ = transaction;
        GoldenManifest model = ContractGoldenManifestPersistence.ResolveGoldenManifestForContractSave(
            contract,
            scope,
            keying,
            authorityPersistBody);
        model.ManifestHash = manifestHashService.ComputeHash(model);
        lock (_lock)
        {
            _store.Add(model);
            if (_store.Count > MaxEntries)
                _store.RemoveRange(0, _store.Count - MaxEntries);
        }
        return Task.FromResult(model);
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

