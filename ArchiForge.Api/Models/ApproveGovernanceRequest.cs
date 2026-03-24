namespace ArchiForge.Api.Models;

public sealed class ApproveGovernanceRequest
{
    public string ReviewedBy { get; set; } = string.Empty;
    public string? ReviewComment { get; set; }
}
