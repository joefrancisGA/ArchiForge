using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class CreateGovernancePromotionRequest
{
    public string RunId { get; set; } = string.Empty;
    public string ManifestVersion { get; set; } = string.Empty;
    public string SourceEnvironment { get; set; } = "dev";
    public string TargetEnvironment { get; set; } = "test";
    public string PromotedBy { get; set; } = string.Empty;
    public string? ApprovalRequestId { get; set; }
    public string? Notes { get; set; }
}
