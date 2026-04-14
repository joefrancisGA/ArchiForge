namespace ArchLucid.Contracts.Governance;

/// <summary>Feature gate for optional pre-commit governance enforcement.</summary>
public sealed class PreCommitGovernanceGateOptions
{
    public const string SectionPath = "ArchLucid:Governance";

    /// <summary>When false (default), <see cref="IPreCommitGovernanceGate"/> is not invoked.</summary>
    public bool PreCommitGateEnabled { get; set; }

    /// <summary>Severity names where the gate warns but does not block (e.g. ["Warning", "Error"]).</summary>
    public string[]? WarnOnlySeverities { get; set; }

    /// <summary>Hours allowed before an approval request is considered SLA-breached. Null = no SLA.</summary>
    public int? ApprovalSlaHours { get; set; }

    /// <summary>Webhook URL for SLA breach notifications (HMAC-signed if secret is set).</summary>
    public string? ApprovalSlaEscalationWebhookUrl { get; set; }

    /// <summary>HMAC-SHA256 secret for signing SLA breach webhook payloads.</summary>
    public string? EscalationWebhookSecret { get; set; }
}
