using ArchLucid.Decisioning.Analysis;
using ArchLucid.Decisioning.Findings.Payloads;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Models;
using ArchLucid.KnowledgeGraph.Models;

namespace ArchLucid.Decisioning.Services;

public class TopologyCoverageFindingEngine(IGraphCoverageAnalyzer analyzer) : IFindingEngine
{
    public string EngineType => "topology-coverage";

    public string Category => "Topology";

    public Task<IReadOnlyList<Finding>> AnalyzeAsync(
        GraphSnapshot graphSnapshot,
        CancellationToken ct)
    {
        TopologyCoverageResult result = analyzer.AnalyzeTopology(graphSnapshot);
        List<Finding> findings = [];

        if (result.TopologyNodeCount == 0)
        {
            findings.Add(new Finding
            {
                FindingSchemaVersion = FindingsSchema.CurrentFindingVersion,
                FindingType = "TopologyCoverageFinding",
                Category = Category,
                EngineType = EngineType,
                Severity = FindingSeverity.Warning,
                Title = "No topology resources were found",
                Rationale = "The graph does not contain any TopologyResource nodes.",
                PayloadType = nameof(TopologyCoverageFindingPayload),
                Payload = new TopologyCoverageFindingPayload
                {
                    TopologyNodeCount = 0,
                    PresentCategories = result.PresentCategories,
                    MissingCategories =
                    [
                        "network",
                        "compute",
                        "storage",
                        "data"
                    ]
                },
                Trace = new ExplainabilityTrace
                {
                    RulesApplied = ["topology-coverage-presence"],
                    DecisionsTaken =
                    [
                        "No TopologyResource nodes found in graph — emitted coverage warning."
                    ],
                    AlternativePathsConsidered =
                    [
                        "Ingest or regenerate graph snapshots until TopologyResource nodes exist for expected categories.",
                        "Verify the graph projection maps infrastructure types into TopologyResource (not only generic service/datastore nodes).",
                        "Treat empty topology as intentional and record scope or policy rationale instead of expanding coverage.",
                    ],
                    Notes = ["Expected categories: network, compute, storage, data"]
                }
            });

            return Task.FromResult<IReadOnlyList<Finding>>(findings);
        }

        if (result.MissingCategories.Count > 0)
            findings.Add(new Finding
            {
                FindingSchemaVersion = FindingsSchema.CurrentFindingVersion,
                FindingType = "TopologyCoverageFinding",
                Category = Category,
                EngineType = EngineType,
                Severity = FindingSeverity.Warning,
                Title = "Topology coverage is incomplete",
                Rationale = "The graph contains topology resources but is missing one or more expected categories.",
                PayloadType = nameof(TopologyCoverageFindingPayload),
                Payload = new TopologyCoverageFindingPayload
                {
                    TopologyNodeCount = result.TopologyNodeCount,
                    PresentCategories = result.PresentCategories,
                    MissingCategories = result.MissingCategories
                },
                RecommendedActions =
                [
                    "Add missing topology categories to the architecture input."
                ],
                Trace = new ExplainabilityTrace
                {
                    GraphNodeIdsExamined = [.. result.TopologyNodeIds],
                    RulesApplied = ["topology-coverage-categories"],
                    DecisionsTaken =
                    [
                        "Compared present topology categories to expected coverage categories."
                    ],
                    AlternativePathsConsidered =
                    [
                        "Add topology-backed resources for missing categories on the next architecture iteration.",
                        "Narrow expected categories when the workload legitimately omits a pillar (document in manifest notes).",
                        "Split mixed workloads into separate runs so each graph can meet category expectations independently.",
                    ],
                    Notes =
                    [
                        $"Present: {string.Join(", ", result.PresentCategories)}",
                        $"Missing: {string.Join(", ", result.MissingCategories)}"
                    ]
                }
            });


        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}
