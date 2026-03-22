using ArchiForge.Core.Comparison;
using ArchiForge.Core.Explanation;
using ArchiForge.Decisioning.Models;
using ArchiForge.Provenance;

namespace ArchiForge.AgentRuntime.Explanation;

public interface IExplanationService
{
    Task<ExplanationResult> ExplainRunAsync(
        GoldenManifest manifest,
        DecisionProvenanceGraph? provenance,
        CancellationToken ct);

    Task<ComparisonExplanationResult> ExplainComparisonAsync(
        ComparisonResult comparison,
        CancellationToken ct);
}
