using ArchiForge.ContextIngestion.Models;
using ArchiForge.KnowledgeGraph.Inference;
using ArchiForge.KnowledgeGraph.Interfaces;
using ArchiForge.KnowledgeGraph.Mapping;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.KnowledgeGraph.Builders;

public class DefaultGraphBuilder : IGraphBuilder
{
    private readonly IGraphNodeFactory _nodeFactory;
    private readonly IGraphEdgeInferer _edgeInferer;

    public DefaultGraphBuilder(
        IGraphNodeFactory nodeFactory,
        IGraphEdgeInferer edgeInferer)
    {
        _nodeFactory = nodeFactory;
        _edgeInferer = edgeInferer;
    }

    public Task<GraphBuildResult> BuildAsync(
        ContextSnapshot contextSnapshot,
        CancellationToken ct)
    {
        var result = new GraphBuildResult();

        var contextNode = new GraphNode
        {
            NodeId = $"context-{contextSnapshot.SnapshotId:N}",
            NodeType = "ContextSnapshot",
            Label = $"Context Snapshot {contextSnapshot.SnapshotId:N}",
            SourceType = "ContextSnapshot",
            SourceId = contextSnapshot.SnapshotId.ToString(),
            Properties = new Dictionary<string, string>
            {
                ["snapshotId"] = contextSnapshot.SnapshotId.ToString(),
                ["runId"] = contextSnapshot.RunId.ToString(),
                ["projectId"] = contextSnapshot.ProjectId
            }
        };

        result.Nodes.Add(contextNode);

        foreach (var item in contextSnapshot.CanonicalObjects)
        {
            result.Nodes.Add(_nodeFactory.CreateNode(item));
        }

        var inferredEdges = _edgeInferer.InferEdges(
            contextSnapshot,
            result.Nodes);

        result.Edges.AddRange(inferredEdges);

        return Task.FromResult(result);
    }
}
