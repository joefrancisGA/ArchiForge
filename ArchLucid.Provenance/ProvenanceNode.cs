namespace ArchiForge.Provenance;

public class ProvenanceNode
{
    public Guid Id { get; set; }
    public ProvenanceNodeType Type { get; set; }

    /// <summary>Domain reference, e.g. FindingId, DecisionId, NodeId, RuleId.</summary>
    public string ReferenceId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public Dictionary<string, string> Metadata { get; set; } = new();
}
