namespace ArchLucid.Provenance.Analysis;

/// <summary>Aggregate provenance traceability completeness for <see cref="DecisionProvenanceGraph"/> decision nodes.</summary>
public sealed class ProvenanceCompletenessResult
{
    /// <summary>Decisions that satisfy finding, graph-context, and rule linkage (see <see cref="ProvenanceCompletenessAnalyzer"/>).</summary>
    public int DecisionsCovered { get; init; }

    /// <summary>Total <see cref="ProvenanceNodeType.Decision"/> nodes in the graph.</summary>
    public int TotalDecisions { get; init; }

    /// <summary>0.0–1.0 — <see cref="DecisionsCovered"/> / <see cref="TotalDecisions"/>; 1.0 when <see cref="TotalDecisions"/> is 0.</summary>
    public double CoverageRatio { get; init; }

    /// <summary><see cref="ProvenanceNode.ReferenceId"/> values for decisions missing at least one required linkage.</summary>
    public IReadOnlyList<string> UncoveredDecisionKeys { get; init; } = [];
}
