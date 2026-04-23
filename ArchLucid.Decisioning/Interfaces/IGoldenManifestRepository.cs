using System.Data;

using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Models;
using Cm = ArchLucid.Contracts.Manifest;

namespace ArchLucid.Decisioning.Interfaces;

public interface IGoldenManifestRepository
{
    Task SaveAsync(
        GoldenManifest manifest,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);

    /// <summary>PR A1 — persist a coordinator-shaped manifest; returns the authority model including computed hash.</summary>
    /// <param name="authorityPersistBody">When non-null, this authority-shaped row (full JSON slices) is persisted as-is after
    /// scope alignment and idempotency-key validation against <paramref name="keying"/>; <paramref name="contract"/> is still required for API symmetry.</param>
    Task<GoldenManifest> SaveAsync(
        Cm.GoldenManifest contract,
        ScopeContext scope,
        SaveContractsManifestOptions keying,
        IManifestHashService manifestHashService,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null,
        GoldenManifest? authorityPersistBody = null);

    Task<GoldenManifest?> GetByIdAsync(ScopeContext scope, Guid manifestId, CancellationToken ct);

    /// <summary>
    /// ADR 0030 — resolves an authority-row manifest whose persisted metadata version matches the coordinator
    /// <c>ManifestVersion</c> string (see <c>MetadataJson</c> <c>Version</c>), within the caller's scope.
    /// </summary>
    Task<GoldenManifest?> GetByContractManifestVersionAsync(ScopeContext scope, string manifestVersion, CancellationToken ct);
}

