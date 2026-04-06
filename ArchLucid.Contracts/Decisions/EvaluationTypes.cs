namespace ArchiForge.Contracts.Decisions;

/// <summary>
/// Well-known values for <see cref="AgentEvaluation.EvaluationType"/>.
/// </summary>
/// <remarks>
/// These constants match the values documented on <see cref="AgentEvaluation.EvaluationType"/>.
/// Use them wherever evaluations are produced or consumed to avoid typos and to make
/// pattern-matching exhaustive.
/// </remarks>
public static class EvaluationTypes
{
    /// <summary>The evaluation supports the target agent's proposed approach.</summary>
    public const string Support = "support";

    /// <summary>The evaluation strengthens confidence in the target agent's proposal.</summary>
    public const string Strengthen = "strengthen";

    /// <summary>The evaluation opposes the target agent's proposed approach.</summary>
    public const string Oppose = "oppose";

    /// <summary>The evaluation flags a concern without fully opposing the proposal.</summary>
    public const string Caution = "caution";
}
