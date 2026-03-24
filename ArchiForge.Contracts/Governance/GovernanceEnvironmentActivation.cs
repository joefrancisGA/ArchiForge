namespace ArchiForge.Contracts.Governance;

public sealed class GovernanceEnvironmentActivation
{
    public string ActivationId { get; set; } = Guid.NewGuid().ToString("N");
    public string RunId { get; set; } = string.Empty;
    public string ManifestVersion { get; set; } = string.Empty;
    public string Environment { get; set; } = GovernanceEnvironment.Dev;
    public bool IsActive { get; set; } = true;
    public DateTime ActivatedUtc { get; set; } = DateTime.UtcNow;
}
