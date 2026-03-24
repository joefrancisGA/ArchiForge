namespace ArchiForge.Contracts.Governance;

public sealed class GovernancePromotionRecord
{
    public string PromotionRecordId { get; set; } = Guid.NewGuid().ToString("N");
    public string RunId { get; set; } = string.Empty;
    public string ManifestVersion { get; set; } = string.Empty;
    public string SourceEnvironment { get; set; } = string.Empty;
    public string TargetEnvironment { get; set; } = string.Empty;
    public string PromotedBy { get; set; } = string.Empty;
    public DateTime PromotedUtc { get; set; } = DateTime.UtcNow;
    public string? ApprovalRequestId { get; set; }
    public string? Notes { get; set; }
}
