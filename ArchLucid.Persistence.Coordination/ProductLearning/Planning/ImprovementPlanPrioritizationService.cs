using System.Globalization;
using System.Text;

using ArchLucid.Contracts.ProductLearning.Planning;

namespace ArchLucid.Persistence.Coordination.ProductLearning.Planning;

/// <inheritdoc />
public sealed class ImprovementPlanPrioritizationService : IImprovementPlanPrioritizationService
{
    private const double WeightSumTolerance = 0.0005;

    public Task<IReadOnlyList<ImprovementPlan>> RankPlansAsync(
        IReadOnlyList<ImprovementPlanScoreInput> items,
        ImprovementPlanPrioritizationWeights weights,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(weights);

        cancellationToken.ThrowIfCancellationRequested();

        ValidateWeights(weights);

        if (items.Count == 0)
        
            return Task.FromResult<IReadOnlyList<ImprovementPlan>>([]);
        

        List<int> frequencies = new(items.Count);
        List<int> severities = new(items.Count);
        List<double> trustStresses = new(items.Count);
        List<int> breadths = new(items.Count);

        foreach (ImprovementPlanScoreInput item in items)
        {
            ArgumentNullException.ThrowIfNull(item.Plan);

            frequencies.Add(Math.Max(0, item.EvidenceSignalCount));
            severities.Add(ComputeSeverityRaw(item));
            trustStresses.Add(ComputeTrustStress(item.AverageTrustScore));
            breadths.Add(Math.Max(0, item.AffectedArtifactTypeCount));
        }

        List<int> normFrequency = NormalizeToThousand(frequencies);
        List<int> normSeverity = NormalizeToThousand(severities);
        List<int> normTrust = NormalizeToThousandDoubles(trustStresses);
        List<int> normBreadth = NormalizeToThousand(breadths);

        List<ImprovementPlan> ranked = new(items.Count);

        for (int i = 0; i < items.Count; i++)
        {
            ImprovementPlanScoreInput item = items[i];
            int nF = normFrequency[i];
            int nS = normSeverity[i];
            int nT = normTrust[i];
            int nB = normBreadth[i];

            double combined =
                weights.Frequency * nF +
                weights.Severity * nS +
                weights.TrustImpact * nT +
                weights.Breadth * nB;

            int priority = (int)Math.Round(combined, MidpointRounding.AwayFromZero);
            priority = Math.Clamp(priority, 0, 1_000_000);

            string explanation = BuildExplanation(
                weights,
                priority,
                nF,
                nS,
                nT,
                nB,
                item);

            ranked.Add(ClonePlanWithPrioritization(item.Plan, priority, explanation));
        }

        List<ImprovementPlan> ordered = ranked
            .OrderByDescending(static p => p.PriorityScore)
            .ThenBy(static p => p.PlanId)
            .ToList();

        return Task.FromResult<IReadOnlyList<ImprovementPlan>>(ordered);
    }

    private static void ValidateWeights(ImprovementPlanPrioritizationWeights weights)
    {
        double sum = weights.Frequency + weights.Severity + weights.TrustImpact + weights.Breadth;

        if (double.IsNaN(sum) || double.IsInfinity(sum) || Math.Abs(sum - 1d) > WeightSumTolerance)
        
            throw new ArgumentException(
                "Weights must sum to 1.0 (±" + WeightSumTolerance + "). Actual sum=" + sum.ToString(CultureInfo.InvariantCulture) + ".",
                nameof(weights));
        

        if (weights.Frequency < 0 || weights.Severity < 0 || weights.TrustImpact < 0 || weights.Breadth < 0)
        
            throw new ArgumentException("Weights must be non-negative.", nameof(weights));
        
    }

    /// <summary>Same spirit as 58R aggregate bad mass: reject &gt; follow-up &gt; revised.</summary>
    private static int ComputeSeverityRaw(ImprovementPlanScoreInput item) =>
        Math.Max(0, item.RejectedCount) * 4 +
        Math.Max(0, item.NeedsFollowUpCount) * 3 +
        Math.Max(0, item.RevisedCount) * 2;

    /// <summary>Higher when trust is lower; null trust → neutral 0.5 stress.</summary>
    private static double ComputeTrustStress(double? averageTrustScore)
    {
        if (!averageTrustScore.HasValue)
        
            return 0.5;
        

        double t = averageTrustScore.Value;

        if (double.IsNaN(t) || double.IsInfinity(t))
        
            return 0.5;
        

        t = Math.Clamp(t, 0d, 1d);

        return 1d - t;
    }

    private static List<int> NormalizeToThousand(IReadOnlyList<int> values)
    {
        if (values.Count == 0)
        
            return [];
        

        int min = values.Min();
        int max = values.Max();

        if (min == max)
        
            return Enumerable.Repeat(1000, values.Count).ToList();
        

        List<int> result = new(values.Count);

        foreach (int v in values)
        {
            double scaled = (v - min) * 1000.0 / (max - min);
            result.Add((int)Math.Round(scaled, MidpointRounding.AwayFromZero));
        }

        return result;
    }

    private static List<int> NormalizeToThousandDoubles(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        
            return [];
        

        double min = values.Min();
        double max = values.Max();

        if (Math.Abs(max - min) < double.Epsilon)
        
            return Enumerable.Repeat(1000, values.Count).ToList();
        

        List<int> result = new(values.Count);

        foreach (double v in values)
        {
            double scaled = (v - min) * 1000.0 / (max - min);
            result.Add((int)Math.Round(scaled, MidpointRounding.AwayFromZero));
        }

        return result;
    }

    private static string BuildExplanation(
        ImprovementPlanPrioritizationWeights weights,
        int priorityScore,
        int normFrequency,
        int normSeverity,
        int normTrustStress,
        int normBreadth,
        ImprovementPlanScoreInput item)
    {
        StringBuilder builder = new();

        builder.Append("priorityScore=");
        builder.Append(priorityScore.ToString(CultureInfo.InvariantCulture));
        builder.Append("; weights frequency=");
        builder.Append(weights.Frequency.ToString(CultureInfo.InvariantCulture));
        builder.Append(" severity=");
        builder.Append(weights.Severity.ToString(CultureInfo.InvariantCulture));
        builder.Append(" trustImpact=");
        builder.Append(weights.TrustImpact.ToString(CultureInfo.InvariantCulture));
        builder.Append(" breadth=");
        builder.Append(weights.Breadth.ToString(CultureInfo.InvariantCulture));
        builder.Append("; normalized0to1000 frequency=");
        builder.Append(normFrequency.ToString(CultureInfo.InvariantCulture));
        builder.Append(" severity=");
        builder.Append(normSeverity.ToString(CultureInfo.InvariantCulture));
        builder.Append(" trustStress=");
        builder.Append(normTrustStress.ToString(CultureInfo.InvariantCulture));
        builder.Append(" breadth=");
        builder.Append(normBreadth.ToString(CultureInfo.InvariantCulture));
        builder.Append("; raw signals=");
        builder.Append(item.EvidenceSignalCount.ToString(CultureInfo.InvariantCulture));
        builder.Append(" rejected=");
        builder.Append(item.RejectedCount.ToString(CultureInfo.InvariantCulture));
        builder.Append(" revised=");
        builder.Append(item.RevisedCount.ToString(CultureInfo.InvariantCulture));
        builder.Append(" followUp=");
        builder.Append(item.NeedsFollowUpCount.ToString(CultureInfo.InvariantCulture));
        builder.Append(" trustAvg=");
        builder.Append(item.AverageTrustScore?.ToString(CultureInfo.InvariantCulture) ?? "null");
        builder.Append(" artifactFacets=");
        builder.Append(item.AffectedArtifactTypeCount.ToString(CultureInfo.InvariantCulture));
        builder.Append('.');

        return builder.ToString();
    }

    private static ImprovementPlan ClonePlanWithPrioritization(
        ImprovementPlan plan,
        int priorityScore,
        string explanation)
    {
        return new ImprovementPlan
        {
            PlanId = plan.PlanId,
            ThemeId = plan.ThemeId,
            Title = plan.Title,
            Description = plan.Description,
            ProposedChanges = plan.ProposedChanges,
            PriorityScore = priorityScore,
            FrequencyScore = plan.FrequencyScore,
            SeverityScore = plan.SeverityScore,
            TrustImpactScore = plan.TrustImpactScore,
            CreatedUtc = plan.CreatedUtc,
            PrioritizationExplanation = explanation,
        };
    }
}
