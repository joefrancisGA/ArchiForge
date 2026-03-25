using ArchiForge.Core.Comparison;
using ArchiForge.Decisioning.Advisory.Analysis;
using ArchiForge.Decisioning.Advisory.Learning;
using ArchiForge.Decisioning.Advisory.Models;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Advisory.Services;

/// <inheritdoc cref="IImprovementAdvisorService" />
public sealed class ImprovementAdvisorService(
    IImprovementSignalAnalyzer signalAnalyzer,
    IRecommendationGenerator recommendationGenerator,
    IRecommendationLearningService learningService) : IImprovementAdvisorService
{
    /// <inheritdoc />
    public async Task<ImprovementPlan> GeneratePlanAsync(
        GoldenManifest manifest,
        FindingsSnapshot findingsSnapshot,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(findingsSnapshot);

        RecommendationLearningProfile? profile = await learningService
            .GetLatestProfileAsync(manifest.TenantId, manifest.WorkspaceId, manifest.ProjectId, ct)
            .ConfigureAwait(false);

        IReadOnlyList<ImprovementSignal> signals = signalAnalyzer.Analyze(manifest, findingsSnapshot);
        IReadOnlyList<ImprovementRecommendation> recommendations = recommendationGenerator.Generate(signals, profile);

        return new ImprovementPlan
        {
            RunId = manifest.RunId,
            Recommendations = recommendations.ToList(),
            SummaryNotes = BuildSummary(recommendations, profile)
        };
    }

    /// <inheritdoc />
    public async Task<ImprovementPlan> GeneratePlanAsync(
        GoldenManifest manifest,
        FindingsSnapshot findingsSnapshot,
        ComparisonResult comparison,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(findingsSnapshot);
        ArgumentNullException.ThrowIfNull(comparison);

        RecommendationLearningProfile? profile = await learningService
            .GetLatestProfileAsync(manifest.TenantId, manifest.WorkspaceId, manifest.ProjectId, ct)
            .ConfigureAwait(false);

        IReadOnlyList<ImprovementSignal> signals = signalAnalyzer.Analyze(manifest, findingsSnapshot, comparison);
        IReadOnlyList<ImprovementRecommendation> recommendations = recommendationGenerator.Generate(signals, profile);

        return new ImprovementPlan
        {
            RunId = manifest.RunId,
            ComparedToRunId = comparison.BaseRunId,
            Recommendations = recommendations.ToList(),
            SummaryNotes = BuildSummary(recommendations, profile)
        };
    }

    private static List<string> BuildSummary(
        IReadOnlyList<ImprovementRecommendation> recommendations,
        RecommendationLearningProfile? profile)
    {
        List<string> notes = new List<string>();

        if (recommendations.Count == 0)
        {
            notes.Add("No significant improvements were identified.");
            return notes;
        }

        notes.Add($"Generated {recommendations.Count} improvement recommendations.");

        if (profile is not null)
            notes.Add("Adaptive prioritization was applied using historical recommendation outcomes.");
        else
            notes.Add("No adaptive learning profile was available. Base prioritization was used.");

        int high = recommendations.Count(x =>
            string.Equals(x.Urgency, "High", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.Urgency, "Critical", StringComparison.OrdinalIgnoreCase));

        notes.Add($"{high} recommendations are high urgency or above.");

        List<string> topCategories = recommendations
            .GroupBy(x => x.Category)
            .OrderByDescending(x => x.Count())
            .Select(x => $"{x.Key}: {x.Count()}")
            .Take(3)
            .ToList();

        notes.AddRange(topCategories);

        return notes;
    }
}
