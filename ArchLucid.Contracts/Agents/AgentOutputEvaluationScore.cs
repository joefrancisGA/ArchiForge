using ArchLucid.Contracts.Common;

namespace ArchLucid.Contracts.Agents;

/// <summary>Structural completeness of one persisted <see cref="AgentExecutionTrace" /> <c>ParsedResultJson</c> payload.</summary>
public sealed class AgentOutputEvaluationScore
{
    /// <summary>Matches <see cref="AgentExecutionTrace.TraceId" />.</summary>
    public string TraceId
    {
        get;
        set;
    } = string.Empty;

    /// <summary>Agent role for the trace row.</summary>
    public AgentType AgentType
    {
        get;
        set;
    }

    /// <summary>Fraction of expected top-level JSON properties present (0.0–1.0).</summary>
    public double StructuralCompletenessRatio
    {
        get;
        set;
    }

    /// <summary>True when <see cref="ParsedResultJson" /> is not a JSON object (parse error or wrong root kind).</summary>
    public bool IsJsonParseFailure
    {
        get;
        set;
    }

    /// <summary>Expected property names missing from the root object (camelCase, as in trace JSON).</summary>
    public IReadOnlyList<string> MissingKeys
    {
        get;
        set;
    } = [];

    /// <summary>Deterministic semantic inspection of claims/findings; null when <see cref="IsJsonParseFailure" />.</summary>
    public AgentOutputSemanticScore? Semantic
    {
        get;
        set;
    }

    /// <summary>True when full prompt/response blob persistence failed for this trace; null if unknown or not attempted.</summary>
    public bool? BlobUploadFailed
    {
        get;
        set;
    }

    /// <summary>Copied from <see cref="AgentExecutionTrace.QualityWarning" /> when present on the persisted trace.</summary>
    public bool QualityWarning
    {
        get;
        set;
    }
}
