namespace ArchiForge.Provenance;

public class ProvenanceEdge
{
    public Guid Id { get; set; }

    public Guid FromNodeId { get; set; }
    public Guid ToNodeId { get; set; }

    public ProvenanceEdgeType Type { get; set; }
}
