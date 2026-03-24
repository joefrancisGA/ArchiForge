namespace ArchiForge.Api.Models;

public sealed class CreateGovernanceApprovalRequest
{
    public string RunId { get; set; } = string.Empty;
    public string ManifestVersion { get; set; } = string.Empty;
    public string SourceEnvironment { get; set; } = "dev";
    public string TargetEnvironment { get; set; } = "test";
    public string? RequestComment { get; set; }
}
