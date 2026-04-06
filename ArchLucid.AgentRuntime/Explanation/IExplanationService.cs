using ArchiForge.Core.Comparison;
using ArchiForge.Core.Explanation;
using ArchiForge.Decisioning.Models;
using ArchiForge.Provenance;

namespace ArchiForge.AgentRuntime.Explanation;

/// <summary>
/// LLM-backed narratives grounded in manifest, optional provenance, or a precomputed <see cref="ComparisonResult"/>.
/// </summary>
/// <remarks>
/// Default implementation: <see cref="ExplanationService"/> (uses <see cref="IAgentCompletionClient"/>). HTTP entry points: <c>ArchiForge.Api.Controllers.ExplanationController</c>, <c>DocxExportController</c> (optional sections).
/// Falls back to heuristic text when the model is unavailable or returns invalid JSON.
/// </remarks>
public interface IExplanationService
{
    /// <summary>
    /// Builds stakeholder-oriented summary and narrative from manifest metadata, derived bullet lists, and optional provenance stats.
    /// </summary>
    /// <param name="manifest">Golden manifest for the run (source of truth for prompts).</param>
    /// <param name="provenance">Optional decision provenance graph; when <see langword="null"/>, provenance section of the prompt notes it is missing.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Structured <see cref="ExplanationResult"/>; never <see langword="null"/>.</returns>
    Task<ExplanationResult> ExplainRunAsync(
        GoldenManifest manifest,
        DecisionProvenanceGraph? provenance,
        CancellationToken ct);

    /// <summary>
    /// Explains base → target changes described by <paramref name="comparison"/> (decisions, requirements, security, topology, cost).
    /// </summary>
    /// <param name="comparison">Output of <see cref="ArchiForge.Decisioning.Comparison.IComparisonService.Compare"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Structured <see cref="ComparisonExplanationResult"/>; never <see langword="null"/>.</returns>
    Task<ComparisonExplanationResult> ExplainComparisonAsync(
        ComparisonResult comparison,
        CancellationToken ct);
}
