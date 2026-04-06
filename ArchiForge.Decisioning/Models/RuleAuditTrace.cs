namespace ArchiForge.Decisioning.Models;

/// <summary>
/// Authority pipeline record of which rules were applied and which findings were accepted or rejected.
/// </summary>
/// <remarks>
/// Distinct from <see cref="ArchiForge.Contracts.Metadata.RunEventTrace"/>, which logs coordinator merge/engine steps for string runs.
/// </remarks>
public class RuleAuditTrace
{
    public Guid TenantId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid DecisionTraceId { get; set; }
    public Guid RunId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public string RuleSetId { get; set; } = string.Empty;
    public string RuleSetVersion { get; set; } = string.Empty;
    public string RuleSetHash { get; set; } = string.Empty;
    public List<string> AppliedRuleIds { get; set; } = [];
    public List<string> AcceptedFindingIds { get; set; } = [];
    public List<string> RejectedFindingIds { get; set; } = [];
    public List<string> Notes { get; set; } = [];
}
