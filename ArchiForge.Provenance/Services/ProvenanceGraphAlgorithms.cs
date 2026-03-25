namespace ArchiForge.Provenance.Services;

/// <summary>Subgraph and neighborhood extraction over <see cref="DecisionProvenanceGraph"/>.</summary>
public static class ProvenanceGraphAlgorithms
{
    /// <summary>
    /// Resolves a route/query key to the internal provenance node id of a <see cref="ProvenanceNodeType.Decision"/> node.
    /// </summary>
    public static bool TryResolveDecisionNodeId(DecisionProvenanceGraph graph, string? decisionKey, out Guid decisionInternalNodeId)
    {
        decisionInternalNodeId = Guid.Empty;
        string key = decisionKey?.Trim() ?? string.Empty;
        if (key.Length == 0)
            return false;

        if (Guid.TryParse(key, out Guid parsedGuid))
        {
            ProvenanceNode? byId = graph.Nodes.FirstOrDefault(n => n.Type == ProvenanceNodeType.Decision && n.Id == parsedGuid);
            if (byId is not null)
            {
                decisionInternalNodeId = byId.Id;
                return true;
            }

            string nFormat = parsedGuid.ToString("N");
            string dFormat = parsedGuid.ToString("D");
            ProvenanceNode? byRefFromGuid = graph.Nodes.FirstOrDefault(n =>
                n.Type == ProvenanceNodeType.Decision &&
                (string.Equals(n.ReferenceId, nFormat, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(n.ReferenceId, dFormat, StringComparison.OrdinalIgnoreCase)));
            if (byRefFromGuid is null) return false;
            decisionInternalNodeId = byRefFromGuid.Id;
            return true;

        }

        ProvenanceNode? byRef = graph.Nodes.FirstOrDefault(n =>
            n.Type == ProvenanceNodeType.Decision &&
            string.Equals(n.ReferenceId, key, StringComparison.OrdinalIgnoreCase));
        if (byRef is null)
            return false;

        decisionInternalNodeId = byRef.Id;
        return true;
    }

    public static DecisionProvenanceGraph ExtractDecisionSubgraph(DecisionProvenanceGraph full, Guid decisionInternalNodeId)
    {
        if (full.Nodes.All(n => n.Id != decisionInternalNodeId))
        {
            return new DecisionProvenanceGraph
            {
                Id = full.Id,
                RunId = full.RunId,
                Nodes = [],
                Edges = []
            };
        }

        HashSet<Guid> includedNodes = [decisionInternalNodeId];
        List<ProvenanceEdge> includedEdges = [];
        
        foreach (ProvenanceEdge edge in full.Edges.Where(edge => edge.FromNodeId == decisionInternalNodeId || edge.ToNodeId == decisionInternalNodeId))
        {
            includedEdges.Add(edge);
            includedNodes.Add(edge.FromNodeId);
            includedNodes.Add(edge.ToNodeId);
        }

        return new DecisionProvenanceGraph
        {
            Id = full.Id,
            RunId = full.RunId,
            Nodes = full.Nodes.Where(n => includedNodes.Contains(n.Id)).ToList(),
            Edges = includedEdges
        };
    }

    /// <summary>
    /// Undirected BFS: <paramref name="depth"/> expansion rounds from <paramref name="startNodeId"/> (0 = start node only).
    /// </summary>
    public static DecisionProvenanceGraph ExtractNeighborhood(DecisionProvenanceGraph full, Guid startNodeId, int depth)
    {
        depth = Math.Clamp(depth, 0, 10);

        if (full.Nodes.All(n => n.Id != startNodeId))
        {
            return new DecisionProvenanceGraph
            {
                Id = full.Id,
                RunId = full.RunId,
                Nodes = [],
                Edges = []
            };
        }

        HashSet<Guid> visited = [startNodeId];
        HashSet<Guid> frontier = [startNodeId];

        for (int i = 0; i < depth; i++)
        {
            HashSet<Guid> next = [];
            foreach (ProvenanceEdge edge in full.Edges)
            {
                if (frontier.Contains(edge.FromNodeId))
                    next.Add(edge.ToNodeId);
                if (frontier.Contains(edge.ToNodeId))
                    next.Add(edge.FromNodeId);
            }

            frontier = next;
            foreach (Guid id in frontier)
                visited.Add(id);
        }

        return new DecisionProvenanceGraph
        {
            Id = full.Id,
            RunId = full.RunId,
            Nodes = full.Nodes.Where(n => visited.Contains(n.Id)).ToList(),
            Edges = full.Edges.Where(e =>
                visited.Contains(e.FromNodeId) &&
                visited.Contains(e.ToNodeId)).ToList()
        };
    }
}
