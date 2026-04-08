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
                    DecisionsTaken =
                    [
                        "No TopologyResource nodes found in graph — emitted coverage warning."
                    ]
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
                    DecisionsTaken =
                    [
                        "Compared present topology categories to expected coverage categories."
                    ]
                }
            });
        

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}
