namespace ArchLucid.Contracts.Agents;

/// <summary>On-demand structural evaluation across traces for one run (not persisted).</summary>
public sealed class AgentOutputEvaluationSummary
{
    public string RunId { get; set; } = string.Empty;

    public DateTime EvaluatedAtUtc { get; set; }

    /// <summary>Rows with <see cref="AgentExecutionTrace.ParseSucceeded"/> and non-empty <see cref="AgentExecutionTrace.ParsedResultJson"/>.</summary>
    public IReadOnlyList<AgentOutputEvaluationScore> Scores { get; set; } = Array.Empty<AgentOutputEvaluationScore>();

    /// <summary>Traces skipped (no parsed JSON to score).</summary>
    public int TracesSkippedCount { get; set; }

    /// <summary>Mean of <see cref="AgentOutputEvaluationScore.StructuralCompletenessRatio"/> over scores where <see cref="AgentOutputEvaluationScore.IsJsonParseFailure"/> is false; null when none.</summary>
    public double? AverageStructuralCompletenessRatio { get; set; }
}
