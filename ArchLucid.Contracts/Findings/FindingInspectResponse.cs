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
}
