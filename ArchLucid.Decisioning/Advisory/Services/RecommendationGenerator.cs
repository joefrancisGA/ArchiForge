using ArchiForge.Decisioning.Advisory.Learning;
using ArchiForge.Decisioning.Advisory.Models;

namespace ArchiForge.Decisioning.Advisory.Services;

/// <summary>
/// Default <see cref="IRecommendationGenerator"/>: maps each <see cref="ImprovementSignal"/> to an <see cref="ImprovementRecommendation"/> using rule-based title/action text, category/scoring heuristics, and optional adaptive scoring.
/// </summary>
public sealed class RecommendationGenerator(IAdaptiveRecommendationScorer adaptiveScorer) : IRecommendationGenerator
{
    /// <inheritdoc />
    /// <remarks>
    /// Urgency is derived from signal severity; base priority from category and severity; <paramref name="profile"/> is passed to <see cref="IAdaptiveRecommendationScorer"/>.
    /// Supporting ids on recommendations come from <see cref="ImprovementSignal.FindingIds"/> and <see cref="ImprovementSignal.DecisionIds"/> (artifacts list is left empty at this stage).
    /// </remarks>
    public IReadOnlyList<ImprovementRecommendation> Generate(
        IReadOnlyList<ImprovementSignal> signals,
        RecommendationLearningProfile? profile = null)
    {
        ArgumentNullException.ThrowIfNull(signals);

        List<ImprovementRecommendation> recommendations = [];
        recommendations.AddRange(from signal in signals
        let baseScore = ComputePriority(signal)
        let urgency = MapUrgency(signal.Severity)
        
        let scoring = adaptiveScorer.Score(new AdaptiveScoringInput { Category = signal.Category, Urgency = urgency, SignalType = signal.SignalType, BasePriorityScore = baseScore }, profile)
        select new ImprovementRecommendation
        {
            Title = BuildTitle(signal),
            Category = signal.Category,
            Rationale = signal.Description,
            SuggestedAction = BuildSuggestedAction(signal),
            Urgency = urgency,
            ExpectedImpact = BuildImpact(signal),
            SupportingFindingIds = signal.FindingIds.ToList(),
            SupportingDecisionIds = signal.DecisionIds.ToList(),
            PriorityScore = scoring.AdaptedPriorityScore
        });

        return recommendations
            .OrderByDescending(x => x.PriorityScore)
            .ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string BuildTitle(ImprovementSignal signal) =>
        signal.SignalType switch
        {
            ImprovementSignalTypes.UncoveredRequirement => "Cover an uncovered requirement",
            ImprovementSignalTypes.SecurityGap => "Close a security protection gap",
            ImprovementSignalTypes.ComplianceGap => "Address a compliance control gap",
            ImprovementSignalTypes.TopologyGap => "Improve topology completeness",
            ImprovementSignalTypes.CostRisk => "Reduce a cost risk",
            ImprovementSignalTypes.SecurityRegression => "Reverse a security regression",
            ImprovementSignalTypes.CostIncrease => "Reduce increased projected cost",
            ImprovementSignalTypes.UnresolvedIssue => $"Resolve: {signal.Title}",
            ImprovementSignalTypes.DecisionRemoved => "Restore or replace removed architecture decision",
            ImprovementSignalTypes.PolicyViolation => "Resolve a manifest policy violation",
            _ => signal.Title
        };

    private static string BuildSuggestedAction(ImprovementSignal signal) =>
        signal.SignalType switch
        {
            ImprovementSignalTypes.UncoveredRequirement =>
                "Add architecture components or decisions that explicitly satisfy the uncovered requirement.",
            ImprovementSignalTypes.SecurityGap => "Introduce or apply missing security controls to the affected resources.",
            ImprovementSignalTypes.ComplianceGap =>
                "Map the required control to architecture resources and add enforcement coverage.",
            ImprovementSignalTypes.TopologyGap => "Add missing topology categories or required platform components.",
            ImprovementSignalTypes.CostRisk =>
                "Review sizing, deployment choices, and service selection to reduce projected cost risk.",
            ImprovementSignalTypes.SecurityRegression =>
                "Review the changed control posture and restore the stronger security baseline where appropriate.",
            ImprovementSignalTypes.CostIncrease =>
                "Review the decision changes that increased cost and consider lower-cost alternatives.",
            ImprovementSignalTypes.UnresolvedIssue =>
                "Triage the issue, assign ownership, and update the architecture or context to close the gap.",
            ImprovementSignalTypes.DecisionRemoved =>
                "Confirm whether the removal was intentional; if not, reinstate the decision or document replacement rationale.",
            ImprovementSignalTypes.PolicyViolation =>
                "Update architecture or context so the policy control is satisfied, or document an approved exemption.",
            _ => "Review this signal and determine the most appropriate architecture correction."
        };

    private static string BuildImpact(ImprovementSignal signal) =>
        signal.Category switch
        {
            "Security" => "Reduces security exposure and improves control posture.",
            "Compliance" => "Improves audit readiness and lowers compliance risk.",
            "Requirement" => "Improves requirement satisfaction and business alignment.",
            "Topology" => "Improves architectural completeness and implementation readiness.",
            "Cost" => "Reduces spend risk and improves financial efficiency.",
            "Risk" => "Reduces delivery and operational risk from open issues.",
            _ => "Improves architecture quality."
        };

    private static string MapUrgency(string severity) =>
        severity.ToLowerInvariant() switch
        {
            "critical" => "Critical",
            "high" => "High",
            "medium" => "Medium",
            _ => "Low"
        };

    private static int ComputePriority(ImprovementSignal signal)
    {
        int score = signal.Category switch
        {
            ImprovementSignalCategories.Security => 90,
            ImprovementSignalCategories.Compliance => 85,
            ImprovementSignalCategories.Requirement => 80,
            ImprovementSignalCategories.Risk => 75,
            ImprovementSignalCategories.Topology => 65,
            ImprovementSignalCategories.Cost => 60,
            _ => 50
        };

        score += SeverityBonus(signal.Severity);

        return score;
    }

    private static int SeverityBonus(string severity)
    {
        if (string.IsNullOrWhiteSpace(severity))
            return 0;

        if (string.Equals(severity, ImprovementSignalSeverities.Critical, StringComparison.OrdinalIgnoreCase))
            return 20;

        if (string.Equals(severity, ImprovementSignalSeverities.High, StringComparison.OrdinalIgnoreCase))
            return 10;

        return string.Equals(severity, ImprovementSignalSeverities.Medium, StringComparison.OrdinalIgnoreCase) ? 5 : 0;
    }
}
