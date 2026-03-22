using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.Decisioning.Models;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.Provenance;

public sealed class ProvenanceBuilder : IProvenanceBuilder
{
    public DecisionProvenanceGraph Build(
        Guid runId,
        FindingsSnapshot findings,
        GraphSnapshot graph,
        GoldenManifest manifest,
        DecisionTrace trace,
        IReadOnlyList<SynthesizedArtifact> artifacts)
    {
        var result = new DecisionProvenanceGraph
        {
            Id = Guid.NewGuid(),
            RunId = runId
        };

        var nodeMap = new Dictionary<string, Guid>(StringComparer.Ordinal);

        var graphNodeIds = new HashSet<string>(graph.Nodes.Select(n => n.NodeId), StringComparer.Ordinal);

        foreach (var n in graph.Nodes)
        {
            AddNode(
                $"graph:{n.NodeId}",
                new ProvenanceNode
                {
                    Type = ProvenanceNodeType.GraphNode,
                    ReferenceId = n.NodeId,
                    Name = string.IsNullOrWhiteSpace(n.Label) ? n.NodeId : n.Label,
                    Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["nodeType"] = n.NodeType,
                        ["category"] = n.Category ?? ""
                    }
                });
        }

        foreach (var f in findings.Findings)
        {
            AddNode(
                $"finding:{f.FindingId}",
                new ProvenanceNode
                {
                    Type = ProvenanceNodeType.Finding,
                    ReferenceId = f.FindingId,
                    Name = f.Title,
                    Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["findingType"] = f.FindingType,
                        ["category"] = f.Category
                    }
                });
        }

        foreach (var ruleId in trace.AppliedRuleIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            AddNode(
                $"rule:{ruleId}",
                new ProvenanceNode
                {
                    Type = ProvenanceNodeType.Rule,
                    ReferenceId = ruleId,
                    Name = ruleId
                });
        }

        foreach (var d in manifest.Decisions)
        {
            AddNode(
                $"decision:{d.DecisionId}",
                new ProvenanceNode
                {
                    Type = ProvenanceNodeType.Decision,
                    ReferenceId = d.DecisionId,
                    Name = d.Title,
                    Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["category"] = d.Category
                    }
                });
        }

        foreach (var a in artifacts)
        {
            AddNode(
                $"artifact:{a.ArtifactId:N}",
                new ProvenanceNode
                {
                    Type = ProvenanceNodeType.Artifact,
                    ReferenceId = a.ArtifactId.ToString("N"),
                    Name = a.Name,
                    Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["artifactType"] = a.ArtifactType,
                        ["format"] = a.Format
                    }
                });
        }

        var manifestNodeId = AddNode(
            $"manifest:{manifest.ManifestId:N}",
            new ProvenanceNode
            {
                Type = ProvenanceNodeType.Manifest,
                ReferenceId = manifest.ManifestId.ToString("N"),
                Name = "GoldenManifest",
                Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["manifestHash"] = manifest.ManifestHash
                }
            });

        // Findings → Decisions (decision supported by findings)
        foreach (var d in manifest.Decisions)
        {
            var decisionKey = $"decision:{d.DecisionId}";
            if (!nodeMap.ContainsKey(decisionKey))
                continue;

            var to = nodeMap[decisionKey];
            foreach (var fId in d.SupportingFindingIds)
            {
                var fk = $"finding:{fId}";
                if (!nodeMap.TryGetValue(fk, out var from))
                    continue;

                AddEdge(from, to, ProvenanceEdgeType.SupportedBy);
            }
        }

        // Graph nodes → Findings (finding influenced by graph context)
        foreach (var f in findings.Findings)
        {
            var fk = $"finding:{f.FindingId}";
            if (!nodeMap.TryGetValue(fk, out var findingNodeId))
                continue;

            foreach (var relatedId in f.RelatedNodeIds)
            {
                var gk = $"graph:{relatedId}";
                if (!graphNodeIds.Contains(relatedId) || !nodeMap.TryGetValue(gk, out var graphNid))
                    continue;

                AddEdge(graphNid, findingNodeId, ProvenanceEdgeType.InfluencedByGraphNode);
            }
        }

        // Rules → Decisions (rules that fired in this trace influenced decisions — v1: cross-product of applied rules × decisions)
        foreach (var d in manifest.Decisions)
        {
            var dk = $"decision:{d.DecisionId}";
            if (!nodeMap.TryGetValue(dk, out var decisionNid))
                continue;

            foreach (var ruleId in trace.AppliedRuleIds.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var rk = $"rule:{ruleId}";
                if (!nodeMap.TryGetValue(rk, out var ruleNid))
                    continue;

                AddEdge(ruleNid, decisionNid, ProvenanceEdgeType.TriggeredByRule);
            }
        }

        // Decisions → Artifacts
        foreach (var a in artifacts)
        {
            var ak = $"artifact:{a.ArtifactId:N}";
            if (!nodeMap.TryGetValue(ak, out var artifactNid))
                continue;

            foreach (var dId in a.ContributingDecisionIds.Distinct(StringComparer.Ordinal))
            {
                var dk = $"decision:{dId}";
                if (!nodeMap.TryGetValue(dk, out var decisionNid))
                    continue;

                AddEdge(decisionNid, artifactNid, ProvenanceEdgeType.ContributedToArtifact);
            }
        }

        // Decisions → Manifest
        foreach (var d in manifest.Decisions)
        {
            var dk = $"decision:{d.DecisionId}";
            if (!nodeMap.TryGetValue(dk, out var decisionNid))
                continue;

            AddEdge(decisionNid, manifestNodeId, ProvenanceEdgeType.ContainedInManifest);
        }

        return result;

        Guid AddNode(string key, ProvenanceNode node)
        {
            if (!nodeMap.TryGetValue(key, out var existing))
            {
                node.Id = Guid.NewGuid();
                result.Nodes.Add(node);
                nodeMap[key] = node.Id;
                return node.Id;
            }

            return existing;
        }

        void AddEdge(Guid from, Guid to, ProvenanceEdgeType type)
        {
            result.Edges.Add(new ProvenanceEdge
            {
                Id = Guid.NewGuid(),
                FromNodeId = from,
                ToNodeId = to,
                Type = type
            });
        }
    }
}
