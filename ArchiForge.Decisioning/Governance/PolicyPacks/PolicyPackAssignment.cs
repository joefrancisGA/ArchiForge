namespace ArchiForge.Decisioning.Governance.PolicyPacks;

public class PolicyPackAssignment
{
    public Guid AssignmentId { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }

    public Guid PolicyPackId { get; set; }
    public string PolicyPackVersion { get; set; } = null!;

    public bool IsEnabled { get; set; } = true;

    public DateTime AssignedUtc { get; set; } = DateTime.UtcNow;
}
