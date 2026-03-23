using ArchiForge.Core.Comparison;
using ArchiForge.Decisioning.Advisory.Models;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Advisory.Services;

/// <summary>
/// Builds an <see cref="ImprovementPlan"/> from a golden manifest and findings, optionally enriched by a manifest comparison to another run.
/// </summary>
/// <remarks>
/// Implementation: <see cref="ImprovementAdvisorService"/>. HTTP entry: <c>ArchiForge.Api.Controllers.AdvisoryController.GetImprovements</c>.
/// Uses <see cref="ArchiForge.Decisioning.Advisory.Analysis.IImprovementSignalAnalyzer"/> and <see cref="IRecommendationGenerator"/>; loads the latest <see cref="Learning.RecommendationLearningProfile"/> when available.
/// </remarks>
public interface IImprovementAdvisorService
{
    /// <summary>Creates a plan for a single-run advisory pass (no baseline comparison).</summary>
    /// <param name="manifest">Authority golden manifest for the target run.</param>
    /// <param name="findingsSnapshot">Findings aligned with the manifest’s snapshot ids.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ImprovementPlan> GeneratePlanAsync(
        GoldenManifest manifest,
        FindingsSnapshot findingsSnapshot,
        CancellationToken ct);

    /// <summary>Creates a plan using diff signals between <paramref name="comparison"/>.Base and the current manifest.</summary>
    /// <param name="manifest">Current run manifest (typically the “target” side of the comparison).</param>
    /// <param name="findingsSnapshot">Findings for the current run.</param>
    /// <param name="comparison">Result of comparing base vs target manifests; drives regression/gap style signals.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ImprovementPlan> GeneratePlanAsync(
        GoldenManifest manifest,
        FindingsSnapshot findingsSnapshot,
        ComparisonResult comparison,
        CancellationToken ct);
}
