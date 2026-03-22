namespace ArchiForge.Decisioning.Governance.PolicyPacks;

public class ResolvedPolicyPack
{
    public Guid PolicyPackId { get; set; }
    public string Name { get; set; } = null!;
    public string Version { get; set; } = null!;
    public string PackType { get; set; } = null!;
    public string ContentJson { get; set; } = null!;
}
