namespace ArchLucid.Application.Determinism;

/// <summary>
///     Captures the comparison outcome for a single replay iteration against the determinism baseline.
/// </summary>
public sealed class DeterminismIterationResult
{
    /// <summary>1-based iteration index within the determinism check run.</summary>
    public int IterationNumber
    {
        get;
        set;
    }

    /// <summary>Run identifier of the replay created for this iteration.</summary>
    public string ReplayRunId
    {
        get;
        set;
    } = string.Empty;

    /// <summary><c>true</c> when this iteration's agent results match the baseline replay exactly.</summary>
    public bool MatchesBaselineAgentResults
    {
        get;
        set;
    }

    /// <summary>
    ///     <c>true</c> when this iteration's manifest matches the baseline replay.
    ///     Also <c>true</c> when neither the baseline nor this iteration produced a manifest (both absent is considered
    ///     matching).
    /// </summary>
    public bool MatchesBaselineManifest
    {
        get;
        set;
    }

    /// <summary>Human-readable descriptions of agent-result drift detected in this iteration. Empty when no drift.</summary>
    public List<string> AgentDriftWarnings
    {
        get;
        set;
    } = [];

    /// <summary>Human-readable descriptions of manifest drift detected in this iteration. Empty when no drift.</summary>
    public List<string> ManifestDriftWarnings
    {
        get;
        set;
    } = [];
}
