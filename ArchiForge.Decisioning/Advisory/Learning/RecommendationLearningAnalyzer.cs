using ArchiForge.Decisioning.Advisory.Workflow;

namespace ArchiForge.Decisioning.Advisory.Learning;

public sealed class RecommendationLearningAnalyzer : IRecommendationLearningAnalyzer
{
    public RecommendationLearningProfile BuildProfile(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        IReadOnlyList<RecommendationRecord> recommendations)
    {
        RecommendationLearningProfile profile = new()
        {
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            GeneratedUtc = DateTime.UtcNow,
            CategoryStats = BuildStats(recommendations, x => x.Category),
            UrgencyStats = BuildStats(recommendations, x => x.Urgency),
            SignalTypeStats = BuildStats(recommendations, InferSignalType)
        };

        profile.CategoryWeights = BuildWeights(profile.CategoryStats);
        profile.UrgencyWeights = BuildWeights(profile.UrgencyStats);
        profile.SignalTypeWeights = BuildWeights(profile.SignalTypeStats);

        profile.Notes.Add($"Analyzed {recommendations.Count} recommendation records.");
        profile.Notes.Add($"Generated {profile.CategoryWeights.Count} category weights.");
        profile.Notes.Add($"Generated {profile.UrgencyWeights.Count} urgency weights.");
        profile.Notes.Add($"Generated {profile.SignalTypeWeights.Count} signal-type weights.");

        return profile;
    }

    private static List<RecommendationOutcomeStats> BuildStats(
        IReadOnlyList<RecommendationRecord> items,
        Func<RecommendationRecord, string> selector)
    {
        return items
            .GroupBy(selector, StringComparer.OrdinalIgnoreCase)
            .Select(group => new RecommendationOutcomeStats
            {
                Key = group.Key,
                ProposedCount = group.Count(),
                AcceptedCount = group.Count(x => x.Status == RecommendationStatus.Accepted),
                RejectedCount = group.Count(x => x.Status == RecommendationStatus.Rejected),
                DeferredCount = group.Count(x => x.Status == RecommendationStatus.Deferred),
                ImplementedCount = group.Count(x => x.Status == RecommendationStatus.Implemented)
            })
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static Dictionary<string, double> BuildWeights(IReadOnlyList<RecommendationOutcomeStats> stats)
    {
        Dictionary<string, double> weights = new(StringComparer.OrdinalIgnoreCase);

        foreach (RecommendationOutcomeStats stat in stats)
        {
            double acceptedScore = stat.AcceptanceRate * 1.5;
            double implementedScore = stat.ImplementationRate * 2.0;
            double deferredPenalty = stat.DeferredRate * 0.7;
            double rejectedPenalty = stat.RejectionRate * 1.0;

            double weight = 1.0 + acceptedScore + implementedScore - deferredPenalty - rejectedPenalty;

            weight = Math.Clamp(weight, 0.5, 2.0);
            weights[stat.Key] = weight;
        }

        return weights;
    }

    private static string InferSignalType(RecommendationRecord record)
    {
        if (record.Category.Equals("Security", StringComparison.OrdinalIgnoreCase))
            return "SecurityGap";

        if (record.Category.Equals("Compliance", StringComparison.OrdinalIgnoreCase))
            return "ComplianceGap";

        if (record.Category.Equals("Requirement", StringComparison.OrdinalIgnoreCase))
            return "UncoveredRequirement";

        if (record.Category.Equals("Topology", StringComparison.OrdinalIgnoreCase))
            return "TopologyGap";

        return record.Category.Equals("Cost", StringComparison.OrdinalIgnoreCase) ? "CostRisk" : "General";
    }
}
