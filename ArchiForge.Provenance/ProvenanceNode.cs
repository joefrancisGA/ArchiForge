namespace ArchiForge.Provenance;

public class ProvenanceNode
{
    public Guid Id { get; set; }
    public ProvenanceNodeType Type { get; set; }

    /// <summary>Domain reference, e.g. FindingId, DecisionId, NodeId, RuleId.</summary>
    public string ReferenceId { get; set; } = default!;

    public string Name { get; set; } = default!;

    public Dictionary<string, string> Metadata { get; set; } = new();
}
