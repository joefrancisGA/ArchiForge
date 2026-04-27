using ArchLucid.Core.Comparison;
using ArchLucid.Core.Explanation;
using ArchLucid.Decisioning.Models;
using ArchLucid.Provenance;

namespace ArchLucid.Application.Explanation;

/// <summary>
///     LLM-backed narratives grounded in manifest, optional provenance, or a precomputed <see cref="ComparisonResult" />.
/// </summary>
/// <remarks>
///     Default implementation: <see cref="ArchLucid.AgentRuntime.Explanation.ExplanationService" />.
/// </remarks>
public interface IExplanationService
{
    /// <summary>
    ///     Builds stakeholder-oriented summary and narrative from manifest metadata, derived bullet lists, and optional
    ///     provenance stats.
    /// </summary>
    Task<ExplanationResult> ExplainRunAsync(
        GoldenManifest manifest,
        DecisionProvenanceGraph? provenance,
        CancellationToken ct);

    /// <summary>
    ///     Explains base → target changes described by <paramref name="comparison" />.
    /// </summary>
    Task<ComparisonExplanationResult> ExplainComparisonAsync(
        ComparisonResult comparison,
        CancellationToken ct);
}
