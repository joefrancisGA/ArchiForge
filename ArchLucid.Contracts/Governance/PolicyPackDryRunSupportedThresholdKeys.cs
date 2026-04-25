namespace ArchLucid.Contracts.Governance;

/// <summary>
///     Stable string constants for the threshold keys that <see cref="PolicyPackDryRunRunItem" /> understands
///     in V1. Unknown keys in <c>proposedThresholds</c> are silently ignored by the dry-run service so
///     callers can experiment with future-shape thresholds without 400-ing the request — but only the keys
///     listed here actually compute a <see cref="PolicyPackDryRunThresholdOutcome" />.
/// </summary>
public static class PolicyPackDryRunSupportedThresholdKeys
{
    /// <summary>Maximum allowed "Critical" severity findings on the run (inclusive cap).</summary>
    public const string MaxCriticalFindings = "maxCriticalFindings";

    /// <summary>Maximum allowed "High" severity findings on the run (inclusive cap).</summary>
    public const string MaxHighFindings = "maxHighFindings";

    /// <summary>Maximum allowed total findings (any severity) on the run (inclusive cap).</summary>
    public const string MaxTotalFindings = "maxTotalFindings";

    /// <summary>Maximum allowed wall-clock minutes from run create to manifest commit (inclusive cap).</summary>
    public const string MaxTimeToCommitMinutes = "maxTimeToCommitMinutes";

    /// <summary>All keys above, in stable display order. Useful for UI rendering and validators.</summary>
    public static IReadOnlyList<string> All
    {
        get;
    } =
    [
        MaxCriticalFindings,
        MaxHighFindings,
        MaxTotalFindings,
        MaxTimeToCommitMinutes,
    ];
}
