using ArchiForge.Decisioning.Analysis;
using ArchiForge.Decisioning.Findings.Payloads;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.Decisioning.Services;

public class SecurityCoverageFindingEngine(IGraphCoverageAnalyzer analyzer) : IFindingEngine
{
    public string EngineType => "security-coverage";

    public string Category => "Security";

    public Task<IReadOnlyList<Finding>> AnalyzeAsync(
        GraphSnapshot graphSnapshot,
        CancellationToken ct)
    {
        var result = analyzer.AnalyzeSecurity(graphSnapshot);
        var findings = new List<Finding>();

        if (result.UnprotectedResourceCount > 0)
        {
            findings.Add(new Finding
            {
                FindingSchemaVersion = FindingsSchema.CurrentFindingVersion,
                FindingType = "SecurityCoverageFinding",
                Category = Category,
                EngineType = EngineType,
                Severity = FindingSeverity.Warning,
                Title = "Some topology resources are not protected by any security baseline nodes",
                Rationale = "Security protection coverage is incomplete for the current topology graph.",
                PayloadType = nameof(SecurityCoverageFindingPayload),
                Payload = new SecurityCoverageFindingPayload
                {
                    SecurityNodeCount = result.SecurityNodeCount,
                    ProtectedResourceCount = result.ProtectedResourceCount,
                    UnprotectedResourceCount = result.UnprotectedResourceCount,
                    UnprotectedResources = result.UnprotectedResources
                },
                RecommendedActions =
                [
                    "Add security baseline declarations or protection mappings for uncovered resources."
                ],
                Trace = new ExplainabilityTrace
                {
                    DecisionsTaken =
                    [
                        "Compared topology resources against PROTECTS edges."
                    ]
                }
            });
        }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}
