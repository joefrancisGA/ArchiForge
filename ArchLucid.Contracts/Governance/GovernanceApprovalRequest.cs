namespace ArchiForge.Contracts.Governance;

/// <summary>
/// Represents a request to promote an architecture run's manifest from one deployment
/// environment to another, subject to a review and approval workflow.
/// </summary>
public sealed class GovernanceApprovalRequest
{
    /// <summary>Unique identifier for this approval request.</summary>
    public string ApprovalRequestId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>The architecture run targeted for promotion.</summary>
    public string RunId { get; set; } = string.Empty;

    /// <summary>The manifest version label being promoted.</summary>
    public string ManifestVersion { get; set; } = string.Empty;

    /// <summary>The environment the run is currently active in.</summary>
    public string SourceEnvironment { get; set; } = GovernanceEnvironment.Dev;

    /// <summary>The environment the run is being promoted to.</summary>
    public string TargetEnvironment { get; set; } = GovernanceEnvironment.Test;

    /// <summary>Current lifecycle status of the request; see <see cref="GovernanceApprovalStatus"/>.</summary>
    public string Status { get; set; } = GovernanceApprovalStatus.Draft;

    /// <summary>Identity of the user who submitted the promotion request.</summary>
    public string RequestedBy { get; set; } = string.Empty;

    /// <summary>Identity of the reviewer, or <see langword="null"/> when not yet reviewed.</summary>
    public string? ReviewedBy { get; set; }

    /// <summary>Optional comment provided by the requester at submission time.</summary>
    public string? RequestComment { get; set; }

    /// <summary>Optional comment provided by the reviewer at decision time.</summary>
    public string? ReviewComment { get; set; }

    /// <summary>UTC timestamp when the request was submitted.</summary>
    public DateTime RequestedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp when the request was reviewed, or <see langword="null"/> when pending.</summary>
    public DateTime? ReviewedUtc { get; set; }
}
