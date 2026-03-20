namespace ArchiForge.Provenance.Services;

public static class ProvenanceGraphViewMapper
{
    public static GraphViewModel ToViewModel(DecisionProvenanceGraph graph) =>
        new()
        {
            Nodes = graph.Nodes
                .Select(n => new GraphNodeVm
                {
                    Id = n.Id.ToString("D"),
                    Label = n.Name,
                    Type = n.Type.ToString(),
                    Metadata = n.Metadata.Count > 0
                        ? new Dictionary<string, string>(n.Metadata)
                        : null
                })
                .ToList(),
            Edges = graph.Edges
                .Select(e => new GraphEdgeVm
                {
                    Source = e.FromNodeId.ToString("D"),
                    Target = e.ToNodeId.ToString("D"),
                    Type = e.Type.ToString()
                })
                .ToList()
        };
}
