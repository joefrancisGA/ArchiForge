using ArchLucid.Decisioning.Analysis;
using ArchLucid.Decisioning.Findings.Payloads;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Models;
using ArchLucid.KnowledgeGraph.Models;

namespace ArchLucid.Decisioning.Services;

public class RequirementCoverageFindingEngine(IGraphCoverageAnalyzer analyzer) : IFindingEngine
{
    public string EngineType => "requirement-coverage";

    public string Category => "Requirement";

    public Task<IReadOnlyList<Finding>> AnalyzeAsync(
        GraphSnapshot graphSnapshot,
        CancellationToken ct)
    {
        RequirementCoverageResult result = analyzer.AnalyzeRequirements(graphSnapshot);
        List<Finding> findings = [];

        if (result.UnrelatedRequirementCount > 0)

            findings.Add(new Finding
            {
                FindingSchemaVersion = FindingsSchema.CurrentFindingVersion,
                FindingType = "RequirementCoverageFinding",
                Category = Category,
                EngineType = EngineType,
                Severity = FindingSeverity.Warning,
                Title = "Some requirements are not related to any topology resources",
                Rationale =
                    "Requirement coverage is incomplete because some requirements do not relate to architecture resources in the graph.",
                PayloadType = nameof(RequirementCoverageFindingPayload),
                Payload = new RequirementCoverageFindingPayload
                {
                    RequirementNodeCount = result.RequirementNodeCount,
                    CoveredRequirementCount = result.RelatedRequirementCount,
                    UncoveredRequirementCount = result.UnrelatedRequirementCount,
                    UncoveredRequirements = result.UncoveredRequirements
                },
                Trace = new ExplainabilityTrace
                {
                    GraphNodeIdsExamined = [.. result.UncoveredRequirements],
                    RulesApplied = ["requirement-coverage-relation"],
                    DecisionsTaken =
                    [
                        "Detected requirement nodes without RELATES_TO edges to topology resources."
                    ],
                    Notes =
                    [
                        $"Total requirements: {result.RequirementNodeCount}",
                        $"Covered: {result.RelatedRequirementCount}, Uncovered: {result.UnrelatedRequirementCount}"
                    ]
                }
            });

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}
