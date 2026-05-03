using System.Text.Json;

namespace ArchLucid.Contracts.Findings;


/// <summary>
/// Read-model for <c>GET /v1/findings/{findingId}/inspect</c> — deterministic explainability without LLM prompt text.
/// </summary>
public sealed class FindingInspectResponse
{
    public string FindingId
    {
        get;
        init;
    } = string.Empty;

    /// <summary>Structured finding payload when relational <c>PayloadJson</c> was persisted; otherwise JSON null.</summary>
    public JsonElement? TypedPayload
    {
        get;
        init;
    }

    /// <summary>Primary rule identifier (first applied rule id from authority decisioning trace, else first trace rule text).</summary>
    public string? DecisionRuleId
    {
        get;
        init;
    }

    /// <summary>Human-oriented rule label (prefer relational trace rule text when present).</summary>
    public string? DecisionRuleName
    {
        get;
        init;
    }

    public IReadOnlyList<FindingInspectEvidenceItem> Evidence
    {
        get;
        init;
    } = [];

    /// <summary>
    /// Best-effort durable audit row written when the authority chain that included this findings snapshot was committed
    /// (<see cref="ArchLucid.Core.Audit.AuditEventTypes.AuthorityCommittedChainPersisted"/>), when SQL audit is enabled.
    /// </summary>
    public Guid? AuditRowId
    {
        get;
        init;
    }

    /// <summary>Authority run that owns the findings snapshot row.</summary>
    public Guid RunId
    {
        get;
        init;
    }

    public string? ManifestVersion
    {
        get;
        init;
    }

    /// <summary>OpenAI / Azure OpenAI deployment name used when the finding was produced (when captured).</summary>
    public string? ModelDeploymentName
    {
        get;
        init;
    }

    /// <summary>Prompt template semantic version from the agent catalog when captured.</summary>
    public string? PromptTemplateVersion
    {
        get;
        init;
    }

    /// <summary>Agent self-rated confidence for the parent result when bridged into persistence.</summary>
    public double? ConfidenceScore
    {
        get;
        init;
    }

    /// <summary>Deterministic evaluation confidence in [0,100] when computed from harness/reference/trace signals.</summary>
    public int? EvaluationConfidenceScore
    {
        get;
        init;
    }

    /// <summary>Mapped bucket for <see cref="EvaluationConfidenceScore" />.</summary>
    public FindingConfidenceLevel? ConfidenceLevel
    {
        get;
        init;
    }

    public FindingHumanReviewStatus HumanReviewStatus
    {
        get;
        init;
    }

    /// <summary>
    /// Ordered list of recommended actions produced by the finding engine.
    /// Populated from <c>dbo.FindingRecommendedActions ORDER BY SortOrder</c>.
    /// Empty when the finding engine produced no explicit actions.
    /// </summary>
    public IReadOnlyList<string> RecommendedActions
    {
        get;
        init;
    } = [];
}
