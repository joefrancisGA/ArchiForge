using ArchLucid.Contracts.Common;

namespace ArchLucid.Application.Diffs;
/// <summary>
///     Maps a <see cref = "ManifestDiffResult"/> to a three-state sponsor badge (ADR 0023).
///     Persisted labels are lowercase: unchanged / changed / breaking.
/// </summary>
public static class ManifestDiffBadgeClassifier
{
    /// <summary>Breaking removed relationships: semantic Hosts→ReadsFrom, Persists→WritesTo, AuthZ→AuthenticatesWith.</summary>
    private static readonly HashSet<string> BreakingRelationshipTypeNames = [nameof(RelationshipType.ReadsFrom), nameof(RelationshipType.WritesTo), nameof(RelationshipType.AuthenticatesWith)];
    /// <inheritdoc cref = "Classify(ManifestDiffResult, bool)"/>
    public static ManifestDiffBadgeState Classify(ManifestDiffResult diff)
    {
        ArgumentNullException.ThrowIfNull(diff);
        return Classify(diff, false);
    }

    /// <param name = "diff"></param>
    /// <param name = "isFirstCommitOnProject">
    ///     When true, returns <see cref = "ManifestDiffBadgeState.Unchanged"/> regardless of
    ///     diff lists.
    /// </param>
    public static ManifestDiffBadgeState Classify(ManifestDiffResult diff, bool isFirstCommitOnProject)
    {
        ArgumentNullException.ThrowIfNull(diff);
        if (isFirstCommitOnProject)
            return ManifestDiffBadgeState.Unchanged;
        if (IsBreaking(diff))
            return ManifestDiffBadgeState.Breaking;
        return !HasAnyStructuralChange(diff) ? ManifestDiffBadgeState.Unchanged : ManifestDiffBadgeState.Changed;
    }

    /// <summary>Lowercase label stored in SQL and Confluence macros.</summary>
    public static string ToPersistedLabel(ManifestDiffBadgeState state)
    {
        return state switch
        {
            ManifestDiffBadgeState.Unchanged => "unchanged",
            ManifestDiffBadgeState.Changed => "changed",
            ManifestDiffBadgeState.Breaking => "breaking",
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)};
    }

    public static bool TryParsePersistedLabel(string? label, out ManifestDiffBadgeState state)
    {
        if (string.Equals(label, "unchanged", StringComparison.OrdinalIgnoreCase))
        {
            state = ManifestDiffBadgeState.Unchanged;
            return true;
        }

        if (string.Equals(label, "changed", StringComparison.OrdinalIgnoreCase))
        {
            state = ManifestDiffBadgeState.Changed;
            return true;
        }

        if (string.Equals(label, "breaking", StringComparison.OrdinalIgnoreCase))
        {
            state = ManifestDiffBadgeState.Breaking;
            return true;
        }

        state = default;
        return false;
    }

    private static bool HasAnyStructuralChange(ManifestDiffResult diff)
    {
        return diff.AddedServices.Count > 0 || diff.RemovedServices.Count > 0 || diff.AddedDatastores.Count > 0 || diff.RemovedDatastores.Count > 0 || diff.AddedRequiredControls.Count > 0 || diff.RemovedRequiredControls.Count > 0 || diff.AddedRelationships.Count > 0 || diff.RemovedRelationships.Count > 0;
    }

    private static bool IsBreaking(ManifestDiffResult diff)
    {
        if (diff.RemovedRequiredControls.Count > 0)
            return true;
        return diff.RemovedDatastores.Count > 0 || diff.RemovedRelationships.Any(r => BreakingRelationshipTypeNames.Contains(r.RelationshipType));
    }
}