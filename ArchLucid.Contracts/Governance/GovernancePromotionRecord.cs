namespace ArchiForge.Contracts.Governance;

/// <summary>
/// Immutable audit record written when a run's manifest is successfully promoted from
/// one deployment environment to another.
/// </summary>
public sealed class GovernancePromotionRecord
{
    /// <summary>Unique identifier for this promotion record.</summary>
    public string PromotionRecordId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>The architecture run that was promoted.</summary>
    public string RunId { get; set; } = string.Empty;

    /// <summary>The manifest version label that was promoted.</summary>
    public string ManifestVersion { get; set; } = string.Empty;

    /// <summary>The environment the run was promoted from.</summary>
    public string SourceEnvironment { get; set; } = string.Empty;

    /// <summary>The environment the run was promoted to.</summary>
    public string TargetEnvironment { get; set; } = string.Empty;

    /// <summary>Identity of the user who performed the promotion.</summary>
    public string PromotedBy { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the promotion was recorded.</summary>
    public DateTime PromotedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The approval request that authorised this promotion, or <see langword="null"/>
    /// when the promotion was performed without a formal approval workflow.
    /// </summary>
    public string? ApprovalRequestId { get; set; }

    /// <summary>Optional free-text notes recorded at promotion time.</summary>
    public string? Notes { get; set; }
}
