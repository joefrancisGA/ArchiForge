using ArchiForge.Decisioning.Governance.PolicyPacks;

namespace ArchiForge.Decisioning.Governance.Resolution;

/// <summary>
/// Canonical string constants for <see cref="PolicyPackAssignment.ScopeLevel"/> (tenant baseline → workspace → project override).
/// </summary>
/// <remarks>
/// Persisted on assignment rows and interpreted by <see cref="EffectiveGovernanceResolver"/> and repositories’ hierarchical queries.
/// API validation should reject unknown strings before assignments reach persistence.
/// </remarks>
public static class GovernanceScopeLevel
{
    /// <summary>Tenant-wide baseline; workspace/project columns on the row are typically empty GUIDs.</summary>
    public const string Tenant = "Tenant";

    /// <summary>Applies to one workspace within the tenant.</summary>
    public const string Workspace = "Workspace";

    /// <summary>Applies to one project (workspace + project must match).</summary>
    public const string Project = "Project";

    /// <summary>All supported levels for validation UI / FluentValidation.</summary>
    public static readonly string[] All = [Tenant, Workspace, Project];

    /// <summary>
    /// Normalizes user/API input to canonical casing, or returns <c>null</c> if the value is not a known level.
    /// </summary>
    /// <param name="value">Raw scope string (may be null, empty, or arbitrary casing).</param>
    /// <returns>
    /// <see cref="Project"/> when <paramref name="value"/> is null/whitespace; otherwise the matching constant, or <c>null</c> if invalid.
    /// </returns>
    /// <remarks>Used by <see cref="PolicyPacks.PolicyPackManagementService.AssignAsync"/> and API validators.</remarks>
    public static string? TryNormalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Project;

        foreach (string level in All)
        {
            if (string.Equals(value, level, StringComparison.OrdinalIgnoreCase))
                return level;
        }

        return null;
    }
}
