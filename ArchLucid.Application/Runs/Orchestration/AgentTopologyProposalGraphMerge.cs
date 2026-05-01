using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.Manifest;
using ArchLucid.KnowledgeGraph;
using ArchLucid.KnowledgeGraph.Models;

namespace ArchLucid.Application.Runs.Orchestration;

/// <summary>
///     Merges Topology agent <see cref="ManifestDeltaProposal" /> into the run's graph so authority commit
///     can project <see cref="ManifestService" /> / <see cref="ManifestDatastore" /> after execute, when the
///     graph from context ingestion had no <see cref="GraphNodeTypes.TopologyResource" /> nodes.
/// </summary>
public static class AgentTopologyProposalGraphMerge
{
    public static GraphSnapshot WithMergedTopologyProposals(
        GraphSnapshot graph,
        IReadOnlyList<AgentResult> results)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(results);

        if (results.Count == 0)
            return graph;

        HashSet<string> seenLabels = new(StringComparer.OrdinalIgnoreCase);

        foreach (GraphNode n in graph.Nodes.Where(n => !string.IsNullOrWhiteSpace(n.Label)))
        {
            seenLabels.Add(n.Label);
        }

        List<GraphNode> added = [];

        foreach (AgentResult result in results)
        {
            if (result.AgentType != AgentType.Topology)
                continue;

            ManifestDeltaProposal? proposal = result.ProposedChanges;
            if (proposal is null)
                continue;

            if (proposal.AddedServices is { Count: > 0 })
            {
                foreach (ManifestService svc in proposal.AddedServices)
                {
                    if (string.IsNullOrWhiteSpace(svc.ServiceName))
                        continue;

                    if (!seenLabels.Add(svc.ServiceName))
                        continue;

                    added.Add(TopologyServiceNode(svc));
                }
            }

            if (proposal.AddedDatastores is not { Count: > 0 })
                continue;

            foreach (ManifestDatastore ds in proposal.AddedDatastores)
            {
                if (string.IsNullOrWhiteSpace(ds.DatastoreName))
                    continue;

                if (!seenLabels.Add(ds.DatastoreName))
                    continue;

                added.Add(TopologyDatastoreNode(ds));
            }
        }

        if (added.Count == 0)
            return graph;

        return new GraphSnapshot
        {
            GraphSnapshotId = graph.GraphSnapshotId,
            ContextSnapshotId = graph.ContextSnapshotId,
            RunId = graph.RunId,
            CreatedUtc = graph.CreatedUtc,
            Nodes = [.. graph.Nodes, .. added],
            Edges = [.. graph.Edges],
            Warnings = [.. graph.Warnings]
        };
    }

    private static GraphNode TopologyServiceNode(ManifestService svc)
    {
        return new GraphNode
        {
            NodeId = !string.IsNullOrWhiteSpace(svc.ServiceId) ? svc.ServiceId : $"svc-{svc.ServiceName}",
            NodeType = GraphNodeTypes.TopologyResource,
            Label = svc.ServiceName,
            Category = GraphTopologyCategories.Compute,
            SourceType = nameof(AgentType.Topology),
            SourceId = "ProposedChanges",
            Properties = EnumProperties("serviceType", svc.ServiceType, "runtimePlatform", svc.RuntimePlatform)
        };
    }

    private static GraphNode TopologyDatastoreNode(ManifestDatastore ds)
    {
        return new GraphNode
        {
            NodeId = !string.IsNullOrWhiteSpace(ds.DatastoreId) ? ds.DatastoreId : $"ds-{ds.DatastoreName}",
            NodeType = GraphNodeTypes.TopologyResource,
            Label = ds.DatastoreName,
            Category = GraphTopologyCategories.Data,
            SourceType = nameof(AgentType.Topology),
            SourceId = "ProposedChanges",
            Properties = EnumProperties("datastoreType", ds.DatastoreType, "runtimePlatform", ds.RuntimePlatform)
        };
    }

    private static Dictionary<string, string> EnumProperties(
        string key1,
        Enum e1,
        string key2,
        Enum e2)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [key1] = e1.ToString(), [key2] = e2.ToString()
        };
    }
}
