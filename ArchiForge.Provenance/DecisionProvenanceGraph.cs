namespace ArchiForge.Provenance;

public class DecisionProvenanceGraph
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }

    public List<ProvenanceNode> Nodes { get; set; } = [];
    public List<ProvenanceEdge> Edges { get; set; } = [];
}
