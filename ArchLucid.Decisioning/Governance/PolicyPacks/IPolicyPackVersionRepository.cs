namespace ArchiForge.Decisioning.Governance.PolicyPacks;

/// <summary>Persistence port for immutable-ish <see cref="PolicyPackVersion"/> rows (versioned <c>ContentJson</c>).</summary>
/// <remarks>
/// Supports upsert-style publish: same (<see cref="PolicyPackVersion.PolicyPackId"/>, <see cref="PolicyPackVersion.Version"/>)
/// pair can be updated in place when re-publishing.
/// </remarks>
public interface IPolicyPackVersionRepository
{
    /// <summary>Inserts a new version row.</summary>
    Task CreateAsync(PolicyPackVersion version, CancellationToken ct);

    /// <summary>Updates an existing version row (e.g. content and <see cref="PolicyPackVersion.IsPublished"/>).</summary>
    Task UpdateAsync(PolicyPackVersion version, CancellationToken ct);

    /// <summary>Looks up a version by pack id and version label.</summary>
    /// <returns>The row or <c>null</c> if the pack has no such version (assign/publish flows depend on this).</returns>
    Task<PolicyPackVersion?> GetByPackAndVersionAsync(
        Guid policyPackId,
        string version,
        CancellationToken ct);

    /// <summary>All versions for a pack, typically newest first (UI / operator tooling).</summary>
    Task<IReadOnlyList<PolicyPackVersion>> ListByPackAsync(
        Guid policyPackId,
        CancellationToken ct);
}
