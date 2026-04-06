using ArchiForge.ContextIngestion.Models;
using ArchiForge.KnowledgeGraph.Inference;
using ArchiForge.KnowledgeGraph.Interfaces;
using ArchiForge.KnowledgeGraph.Mapping;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.KnowledgeGraph.Builders;

public class DefaultGraphBuilder(
    IGraphNodeFactory nodeFactory,
    IGraphEdgeInferer edgeInferer)
    : IGraphBuilder
{
    public Task<GraphBuildResult> BuildAsync(
        ContextSnapshot contextSnapshot,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(contextSnapshot);

        GraphBuildResult result = new();

        GraphNode contextNode = new()
        {
            NodeId = $"context-{contextSnapshot.SnapshotId:N}",
            NodeType = GraphNodeTypes.ContextSnapshot,
            Label = $"Context Snapshot {contextSnapshot.SnapshotId:N}",
            SourceType = GraphNodeTypes.ContextSnapshot,
            SourceId = contextSnapshot.SnapshotId.ToString(),
            Properties = new Dictionary<string, string>
            {
                ["snapshotId"] = contextSnapshot.SnapshotId.ToString(),
                ["runId"] = contextSnapshot.RunId.ToString(),
                ["projectId"] = contextSnapshot.ProjectId
            }
        };

        result.Nodes.Add(contextNode);

        foreach (CanonicalObject item in contextSnapshot.CanonicalObjects)
        
            result.Nodes.Add(nodeFactory.CreateNode(item));
        

        IReadOnlyList<GraphEdge> inferredEdges = edgeInferer.InferEdges(
            contextSnapshot,
            result.Nodes);

        result.Edges.AddRange(inferredEdges);

        return Task.FromResult(result);
    }
}
