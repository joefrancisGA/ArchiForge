using System.Data;

using ArchLucid.Core.Scoping;

using Cm = ArchLucid.Contracts.Manifest;

namespace ArchLucid.Application.Authority;

/// <summary>
/// Persists <c>dbo.ContextSnapshots</c> → <c>dbo.GraphSnapshots</c> → <c>dbo.FindingsSnapshots</c> →
/// <c>dbo.DecisioningTraces</c> → <c>dbo.GoldenManifests</c> so demo seed and replay satisfy authority FKs.
/// </summary>
public interface IAuthorityCommittedManifestChainWriter
{
    /// <summary>
    /// Inserts the full authority chain for a committed manifest keyed by <paramref name="chainIds"/>.
    /// When <paramref name="connection"/> is supplied, all writes enlist in <paramref name="transaction"/>.
    /// </summary>
    Task<AuthorityManifestPersistResult> PersistCommittedChainAsync(
        ScopeContext scope,
        Guid authorityRunId,
        string projectSlug,
        Cm.GoldenManifest contract,
        AuthorityChainKeying chainIds,
        DateTime createdUtc,
        bool richFindingsAndGraph,
        CancellationToken cancellationToken,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);
}
