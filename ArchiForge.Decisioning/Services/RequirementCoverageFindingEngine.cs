using ArchiForge.Decisioning.Analysis;
using ArchiForge.Decisioning.Findings.Payloads;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.Decisioning.Services;

public class RequirementCoverageFindingEngine(IGraphCoverageAnalyzer analyzer) : IFindingEngine
{
    public string EngineType => "requirement-coverage";

    public string Category => "Requirement";

    public Task<IReadOnlyList<Finding>> AnalyzeAsync(
        GraphSnapshot graphSnapshot,
        CancellationToken ct)
    {
        RequirementCoverageResult result = analyzer.AnalyzeRequirements(graphSnapshot);
        List<Finding> findings = new();

        if (result.UnrelatedRequirementCount > 0)
        {
            findings.Add(new Finding
            {
                FindingSchemaVersion = FindingsSchema.CurrentFindingVersion,
                FindingType = "RequirementCoverageFinding",
                Category = Category,
                EngineType = EngineType,
                Severity = FindingSeverity.Warning,
                Title = "Some requirements are not related to any topology resources",
                Rationale = "Requirement coverage is incomplete because some requirements do not relate to architecture resources in the graph.",
                PayloadType = nameof(RequirementCoverageFindingPayload),
                Payload = new RequirementCoverageFindingPayload
                {
                    RequirementNodeCount = result.RequirementNodeCount,
                    CoveredRequirementCount = result.RelatedRequirementCount,
                    UncoveredRequirementCount = result.UnrelatedRequirementCount,
                    UncoveredRequirements = result.UncoveredRequirements
                }
            });
        }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}
