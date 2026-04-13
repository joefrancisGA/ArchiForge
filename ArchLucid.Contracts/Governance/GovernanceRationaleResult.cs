namespace ArchLucid.Contracts.Governance;

/// <summary>
/// Deterministic governance review context for an approval request (no LLM): joins workflow state to lineage signals.
/// </summary>
public sealed class GovernanceRationaleResult
{
    public int SchemaVersion { get; set; } = 1;

    public string ApprovalRequestId { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public List<string> Bullets { get; set; } = [];
}
