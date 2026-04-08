namespace ArchLucid.Decisioning.Findings;

/// <summary>Per-finding explainability trace population score.</summary>
public sealed class TraceCompletenessScore
{
    public string FindingId { get; init; } = null!;

    public string EngineType { get; init; } = null!;

    public bool HasGraphNodeIds { get; init; }

    public bool HasRulesApplied { get; init; }

    public bool HasDecisionsTaken { get; init; }

    public bool HasAlternativePaths { get; init; }

    public bool HasNotes { get; init; }

    /// <summary>Count of populated fields out of 5.</summary>
    public int PopulatedFieldCount { get; init; }

    /// <summary>0.0 to 1.0 — fraction of the 5 trace fields that are non-empty.</summary>
    public double CompletenessRatio { get; init; }
}
