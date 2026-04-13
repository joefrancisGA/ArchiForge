namespace ArchLucid.Decisioning.Models;

public class ExplainabilityTrace
{
    /// <summary>
    /// Optional correlation to a persisted agent execution trace id (32-char hex, no dashes), when the engine records one.
    /// </summary>
    public string? SourceAgentExecutionTraceId { get; set; }

    public List<string> GraphNodeIdsExamined { get; set; } = [];
    public List<string> RulesApplied { get; set; } = [];
    public List<string> DecisionsTaken { get; set; } = [];
    public List<string> AlternativePathsConsidered { get; set; } = [];
    public List<string> Notes { get; set; } = [];
}

