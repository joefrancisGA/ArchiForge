namespace ArchiForge.Contracts.Governance;

public sealed class GovernanceApprovalRequest
{
    public string ApprovalRequestId { get; set; } = Guid.NewGuid().ToString("N");
    public string RunId { get; set; } = string.Empty;
    public string ManifestVersion { get; set; } = string.Empty;
    public string SourceEnvironment { get; set; } = GovernanceEnvironment.Dev;
    public string TargetEnvironment { get; set; } = GovernanceEnvironment.Test;
    public string Status { get; set; } = GovernanceApprovalStatus.Draft;
    public string RequestedBy { get; set; } = string.Empty;
    public string? ReviewedBy { get; set; }
    public string? RequestComment { get; set; }
    public string? ReviewComment { get; set; }
    public DateTime RequestedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedUtc { get; set; }
}
