namespace ArchiForge.Decisioning.Governance.PolicyPacks;

public class PolicyPackVersion
{
    public Guid PolicyPackVersionId { get; set; } = Guid.NewGuid();
    public Guid PolicyPackId { get; set; }

    public string Version { get; set; } = null!;
    public string ContentJson { get; set; } = null!;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public bool IsPublished { get; set; }
}
