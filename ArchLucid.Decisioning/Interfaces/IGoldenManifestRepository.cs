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
    Task<GoldenManifest> SaveAsync(
        Cm.GoldenManifest contract,
        ScopeContext scope,
        SaveContractsManifestOptions keying,
        IManifestHashService manifestHashService,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);

    Task<GoldenManifest?> GetByIdAsync(ScopeContext scope, Guid manifestId, CancellationToken ct);
}

