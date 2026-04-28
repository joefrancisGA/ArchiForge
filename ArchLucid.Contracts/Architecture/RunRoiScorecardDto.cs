namespace ArchLucid.Contracts.Architecture;

/// <summary>Directional manual-hours estimate derived from authoritative run aggregates (not persisted).</summary>
public sealed class RunRoiScorecardDto
{
    /// <summary>Run identifier (<see cref="string"/> GUID).</summary>
    public required string RunId
    {
        get;
        init;
    }

    /// <summary>Sum of <see cref="ArchLucid.Contracts.Agents.AgentResult.Findings" /> counts.</summary>
    public int AgentFindingTotalCount
    {
        get;
        init;
    }

    public int CompletedAgentResultCount
    {
        get;
        init;
    }

    /// <summary>Services + datastores + relationships when a golden manifest exists.</summary>
    public int ManifestModeledElementApproxCount
    {
        get;
        init;
    }

    public int DecisionTraceCount
    {
        get;
        init;
    }

    /// <summary>
    ///     Estimated equivalent manual analyst hours for comparable packaging work (configured multipliers —
    ///     see <c>Architecture:RunRoiEstimator</c>).
    /// </summary>
    public double EstimatedManualHoursSaved
    {
        get;
        init;
    }

    /// <summary>Stable explanation string for auditors (no LLM).</summary>
    public required string ComputationNotes
    {
        get;
        init;
    }

    /// <summary>UTC when the estimate was computed.</summary>
    public DateTime EstimatedUtc
    {
        get;
        init;
    }
}
