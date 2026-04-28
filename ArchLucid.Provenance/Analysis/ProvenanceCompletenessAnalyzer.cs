namespace ArchLucid.Provenance.Analysis;

/// <summary>
///     Measures how many golden-manifest decisions have full traceability in a <see cref="DecisionProvenanceGraph" />:
///     a <see cref="ProvenanceEdgeType.SupportedBy" /> finding, a <see cref="ProvenanceEdgeType.TriggeredByRule" /> rule,
///     and graph context via <see cref="ProvenanceEdgeType.InfluencedByGraphNode" /> into at least one supporting finding.
/// </summary>
/// <remarks>
///     v1 provenance only materializes direct GraphNode→Finding <see cref="ProvenanceEdgeType.InfluencedByGraphNode" />
///     edges
///     (<see cref="ProvenanceBuilder" />); there is no multi-hop graph walk here.
/// </remarks>
public static class ProvenanceCompletenessAnalyzer
{
    /// <summary>Analyzes decision coverage for the given <paramref name="graph" />.</summary>
    public static ProvenanceCompletenessResult Analyze(DecisionProvenanceGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        // Deserialization or hand-built graphs can leave list properties null; treat as empty.
        IReadOnlyList<ProvenanceNode> nodes = graph.Nodes ?? [];
        IReadOnlyList<ProvenanceEdge> edges = graph.Edges ?? [];

        Dictionary<Guid, ProvenanceNode> nodeById = nodes.ToDictionary(n => n.Id, n => n);

        ILookup<Guid, ProvenanceEdge> inboundByTarget = edges.ToLookup(e => e.ToNodeId);

        List<ProvenanceNode> decisionNodes = nodes
            .Where(n => n.Type == ProvenanceNodeType.Decision)
            .ToList();

        if (decisionNodes.Count == 0)

            return new ProvenanceCompletenessResult
            {
                DecisionsCovered = 0,
                TotalDecisions = 0,
                CoverageRatio = 1.0,
                UncoveredDecisionKeys = []
            };


        List<(ProvenanceNode Node, bool Covered)> evaluated = decisionNodes
            .Select(d => (Node: d, Covered: IsDecisionFullyCovered(d, inboundByTarget, nodeById, edges)))
            .ToList();

        int covered = evaluated.Count(x => x.Covered);

        List<string> uncoveredKeys = evaluated
            .Where(x => !x.Covered)
            .Select(x => x.Node.ReferenceId)
            .ToList();

        double ratio = covered / (double)decisionNodes.Count;

        return new ProvenanceCompletenessResult
        {
            DecisionsCovered = covered,
            TotalDecisions = decisionNodes.Count,
            CoverageRatio = ratio,
            UncoveredDecisionKeys = uncoveredKeys
        };
    }

    private static bool IsDecisionFullyCovered(
        ProvenanceNode decision,
        ILookup<Guid, ProvenanceEdge> inboundByTarget,
        Dictionary<Guid, ProvenanceNode> nodeById,
        IReadOnlyList<ProvenanceEdge> allEdges)
    {
        IEnumerable<ProvenanceEdge> inbound = inboundByTarget[decision.Id];

        return HasInboundOfTypeFromNodeType(inbound, nodeById, ProvenanceEdgeType.SupportedBy,
                   ProvenanceNodeType.Finding)
               && HasInboundOfTypeFromNodeType(inbound, nodeById, ProvenanceEdgeType.TriggeredByRule,
                   ProvenanceNodeType.Rule)
               && SupportingFindingHasGraphInfluence(decision.Id, inboundByTarget, nodeById, allEdges);
    }

    private static bool HasInboundOfTypeFromNodeType(
        IEnumerable<ProvenanceEdge> inbound,
        Dictionary<Guid, ProvenanceNode> nodeById,
        ProvenanceEdgeType edgeType,
        ProvenanceNodeType fromNodeType)
    {
        return inbound.Any(e => e.Type == edgeType
                                && nodeById.TryGetValue(e.FromNodeId, out ProvenanceNode? from)
                                && from.Type == fromNodeType);
    }

    /// <summary>
    ///     True when some finding linked to the decision via <see cref="ProvenanceEdgeType.SupportedBy" /> has at least one
    ///     <see cref="ProvenanceEdgeType.InfluencedByGraphNode" /> inbound edge from a
    ///     <see cref="ProvenanceNodeType.GraphNode" />.
    /// </summary>
    private static bool SupportingFindingHasGraphInfluence(
        Guid decisionId,
        ILookup<Guid, ProvenanceEdge> inboundByTarget,
        Dictionary<Guid, ProvenanceNode> nodeById,
        IReadOnlyList<ProvenanceEdge> allEdges)
    {
        IEnumerable<Guid> supportingFindingIds = inboundByTarget[decisionId]
            .Where(e => e.Type == ProvenanceEdgeType.SupportedBy)
            .Select(e => e.FromNodeId)
            .Where(fid => nodeById.TryGetValue(fid, out ProvenanceNode? n) && n.Type == ProvenanceNodeType.Finding);

        return supportingFindingIds.Any(findingId => FindingHasInboundGraphInfluence(findingId, allEdges, nodeById));
    }

    private static bool FindingHasInboundGraphInfluence(
        Guid findingNodeId,
        IReadOnlyList<ProvenanceEdge> allEdges,
        Dictionary<Guid, ProvenanceNode> nodeById)
    {
        return allEdges.Any(e => e.Type == ProvenanceEdgeType.InfluencedByGraphNode
                                 && e.ToNodeId == findingNodeId
                                 && nodeById.TryGetValue(e.FromNodeId, out ProvenanceNode? from)
                                 && from.Type == ProvenanceNodeType.GraphNode);
    }
}
