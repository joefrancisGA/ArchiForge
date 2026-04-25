using System.Data;

namespace ArchLucid.Decisioning.Governance.PolicyPacks;

/// <summary>Persistence port for immutable-ish <see cref="PolicyPackVersion" /> rows (versioned <c>ContentJson</c>).</summary>
/// <remarks>
///     Supports upsert-style publish: same (<see cref="PolicyPackVersion.PolicyPackId" />,
///     <see cref="PolicyPackVersion.Version" />)
///     pair can be updated in place when re-publishing.
/// </remarks>
public interface IPolicyPackVersionRepository
{
    /// <summary>Inserts a new version row.</summary>
    Task CreateAsync(
        PolicyPackVersion version,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);

    /// <summary>Updates an existing version row (e.g. content and <see cref="PolicyPackVersion.IsPublished" />).</summary>
    Task UpdateAsync(PolicyPackVersion version, CancellationToken ct);

    /// <summary>Looks up a version by pack id and version label.</summary>
    /// <returns>The row or <c>null</c> if the pack has no such version (assign/publish flows depend on this).</returns>
    Task<PolicyPackVersion?> GetByPackAndVersionAsync(
        Guid policyPackId,
        string version,
        CancellationToken ct);

    /// <summary>
    ///     Atomically publishes version content under a single transactional read-modify-write so concurrent publishes
    ///     of the same (<paramref name="policyPackId" />, <paramref name="version" />) cannot create duplicate rows.
    /// </summary>
    /// <returns>
    ///     The persisted row and the prior <see cref="PolicyPackVersion.ContentJson" /> when an existing row was updated;
    ///     otherwise <c>null</c>.
    /// </returns>
    Task<(PolicyPackVersion Version, string? PreviousContentJson)> UpsertPublishedVersionAsync(
        Guid policyPackId,
        string version,
        string contentJson,
        CancellationToken ct);

    /// <summary>All versions for a pack, typically newest first (UI / operator tooling).</summary>
    Task<IReadOnlyList<PolicyPackVersion>> ListByPackAsync(
        Guid policyPackId,
        CancellationToken ct);
}
