namespace ArchiForge.Decisioning.Governance.PolicyPacks;

public class PolicyPack
{
    public Guid PolicyPackId { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }

    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string PackType { get; set; } = PolicyPackType.BuiltIn;
    public string Status { get; set; } = PolicyPackStatus.Draft;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ActivatedUtc { get; set; }

    public string CurrentVersion { get; set; } = "1.0.0";
}
