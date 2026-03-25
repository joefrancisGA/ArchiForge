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
        DecisionProvenanceGraph result = new DecisionProvenanceGraph
        {
            Id = Guid.NewGuid(),
            RunId = runId
        };

        Dictionary<string, Guid> nodeMap = new Dictionary<string, Guid>(StringComparer.Ordinal);

        HashSet<string> graphNodeIds = new HashSet<string>(graph.Nodes.Select(n => n.NodeId), StringComparer.Ordinal);

        foreach (GraphNode n in graph.Nodes)
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

        foreach (Finding f in findings.Findings)
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

        foreach (string ruleId in trace.AppliedRuleIds.Distinct(StringComparer.OrdinalIgnoreCase))
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

        foreach (ResolvedArchitectureDecision d in manifest.Decisions)
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

        foreach (SynthesizedArtifact a in artifacts)
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

        Guid manifestNodeId = AddNode(
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
        foreach (ResolvedArchitectureDecision d in manifest.Decisions)
        {
            string decisionKey = $"decision:{d.DecisionId}";
            if (!nodeMap.ContainsKey(decisionKey))
                continue;

            Guid to = nodeMap[decisionKey];
            foreach (string fId in d.SupportingFindingIds)
            {
                string fk = $"finding:{fId}";
                if (!nodeMap.TryGetValue(fk, out Guid from))
                    continue;

                AddEdge(from, to, ProvenanceEdgeType.SupportedBy);
            }
        }

        // Graph nodes → Findings (finding influenced by graph context)
        foreach (Finding f in findings.Findings)
        {
            string fk = $"finding:{f.FindingId}";
            if (!nodeMap.TryGetValue(fk, out Guid findingNodeId))
                continue;

            foreach (string relatedId in f.RelatedNodeIds)
            {
                string gk = $"graph:{relatedId}";
                if (!graphNodeIds.Contains(relatedId) || !nodeMap.TryGetValue(gk, out Guid graphNid))
                    continue;

                AddEdge(graphNid, findingNodeId, ProvenanceEdgeType.InfluencedByGraphNode);
            }
        }

        // Rules → Decisions (rules that fired in this trace influenced decisions — v1: cross-product of applied rules × decisions)
        foreach (ResolvedArchitectureDecision d in manifest.Decisions)
        {
            string dk = $"decision:{d.DecisionId}";
            if (!nodeMap.TryGetValue(dk, out Guid decisionNid))
                continue;

            foreach (string ruleId in trace.AppliedRuleIds.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                string rk = $"rule:{ruleId}";
                if (!nodeMap.TryGetValue(rk, out Guid ruleNid))
                    continue;

                AddEdge(ruleNid, decisionNid, ProvenanceEdgeType.TriggeredByRule);
            }
        }

        // Decisions → Artifacts
        foreach (SynthesizedArtifact a in artifacts)
        {
            string ak = $"artifact:{a.ArtifactId:N}";
            if (!nodeMap.TryGetValue(ak, out Guid artifactNid))
                continue;

            foreach (string dId in a.ContributingDecisionIds.Distinct(StringComparer.Ordinal))
            {
                string dk = $"decision:{dId}";
                if (!nodeMap.TryGetValue(dk, out Guid decisionNid))
                    continue;

                AddEdge(decisionNid, artifactNid, ProvenanceEdgeType.ContributedToArtifact);
            }
        }

        // Decisions → Manifest
        foreach (ResolvedArchitectureDecision d in manifest.Decisions)
        {
            string dk = $"decision:{d.DecisionId}";
            if (!nodeMap.TryGetValue(dk, out Guid decisionNid))
                continue;

            AddEdge(decisionNid, manifestNodeId, ProvenanceEdgeType.ContainedInManifest);
        }

        return result;

        Guid AddNode(string key, ProvenanceNode node)
        {
            if (!nodeMap.TryGetValue(key, out Guid existing))
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
