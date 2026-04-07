using System.Globalization;

using ArchLucid.Contracts.Evolution;
using ArchLucid.Contracts.ProductLearning.Planning;

namespace ArchLucid.Persistence.Coordination.Evolution;

/// <summary>
/// Deterministic projection from 59R <see cref="ProductLearningImprovementPlanRecord"/> to 60R <see cref="CandidateChangeSet"/> instances (no side effects).
/// </summary>
public sealed class CandidateChangeSetService : ICandidateChangeSetService
{
    /// <inheritdoc />
    public IReadOnlyList<CandidateChangeSet> MapFromImprovementPlan(
        ProductLearningImprovementPlanRecord plan,
        ProductLearningImprovementThemeRecord? theme)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (plan.ActionSteps is null)
        {
            throw new ArgumentException("ActionSteps cannot be null.", nameof(plan));
        }

        IReadOnlyList<CandidateChangeSetStep> orderedSteps = OrderSteps(plan.ActionSteps);
        IReadOnlyList<ChangeSetAffectedComponent> components = BuildAffectedComponents(plan, theme);
        ExpectedImpact impact = BuildExpectedImpact(plan, theme);
        DateTime createdUtc = plan.CreatedUtc;

        List<CandidateChangeSet> results = [];

        results.Add(
            BuildAggregateChangeSet(
                plan,
                orderedSteps,
                components,
                impact,
                createdUtc));

        if (orderedSteps.Count > 1)
        {
            foreach (CandidateChangeSetStep step in orderedSteps)
            {
                results.Add(
                    BuildStepSliceChangeSet(
                        plan,
                        step,
                        components,
                        impact,
                        createdUtc));
            }
        }

        return results;
    }

    private static IReadOnlyList<CandidateChangeSetStep> OrderSteps(
        IReadOnlyList<ProductLearningImprovementPlanActionStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);

        return steps
            .Select(static s => MapStep(s))
            .OrderBy(static s => s.Ordinal)
            .ThenBy(static s => s.ActionType, StringComparer.Ordinal)
            .ThenBy(static s => s.Description, StringComparer.Ordinal)
            .ToList();
    }

    private static CandidateChangeSetStep MapStep(ProductLearningImprovementPlanActionStep step)
    {
        ArgumentNullException.ThrowIfNull(step);

        return new CandidateChangeSetStep
        {
            Ordinal = step.Ordinal,
            ActionType = step.ActionType,
            Description = step.Description,
            AcceptanceCriteria = step.AcceptanceCriteria,
        };
    }

    private static IReadOnlyList<ChangeSetAffectedComponent> BuildAffectedComponents(
        ProductLearningImprovementPlanRecord plan,
        ProductLearningImprovementThemeRecord? theme)
    {
        if (theme is not null)
        {
            List<ChangeSetAffectedComponent> list =
            [
                new ChangeSetAffectedComponent
                {
                    ComponentKey = theme.ThemeKey,
                    DisplayName = theme.Title,
                    WorkflowArea = theme.AffectedArtifactTypeOrWorkflowArea,
                },
            ];

            if (!string.IsNullOrWhiteSpace(theme.PatternKey))
            {
                string patternKey = theme.PatternKey.Trim();

                list.Add(
                    new ChangeSetAffectedComponent
                    {
                        ComponentKey = patternKey,
                        DisplayName = patternKey,
                        WorkflowArea = theme.AffectedArtifactTypeOrWorkflowArea,
                    });
            }

            return list;
        }

        return
        [
            new ChangeSetAffectedComponent
            {
                ComponentKey = plan.PlanId.ToString("N"),
                DisplayName = plan.Title,
                WorkflowArea = "ImprovementPlan",
            },
        ];
    }

    private static ExpectedImpact BuildExpectedImpact(
        ProductLearningImprovementPlanRecord plan,
        ProductLearningImprovementThemeRecord? theme)
    {
        string summary = !string.IsNullOrWhiteSpace(plan.PriorityExplanation)
            ? plan.PriorityExplanation.Trim()
            : plan.Summary.Trim();

        string rationale = BuildImpactRationale(plan, theme);

        return new ExpectedImpact
        {
            Summary = summary,
            Rationale = string.IsNullOrEmpty(rationale) ? null : rationale,
        };
    }

    private static string BuildImpactRationale(
        ProductLearningImprovementPlanRecord plan,
        ProductLearningImprovementThemeRecord? theme)
    {
        if (theme is null)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "PriorityScore={0}; PlanStatus={1}",
                plan.PriorityScore,
                plan.Status);
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            "PriorityScore={0}; PlanStatus={1}; ThemeSeverity={2}; EvidenceSignals={3}; DistinctRuns={4}",
            plan.PriorityScore,
            plan.Status,
            theme.SeverityBand,
            theme.EvidenceSignalCount,
            theme.DistinctRunCount);
    }

    private static CandidateChangeSet BuildAggregateChangeSet(
        ProductLearningImprovementPlanRecord plan,
        IReadOnlyList<CandidateChangeSetStep> orderedSteps,
        IReadOnlyList<ChangeSetAffectedComponent> components,
        ExpectedImpact impact,
        DateTime createdUtc)
    {
        string description = BuildAggregateDescription(plan);

        return new CandidateChangeSet
        {
            ChangeSetId = CandidateChangeSetDeterministicIds.AggregateChangeSetId(plan.PlanId),
            SourcePlanId = plan.PlanId,
            Description = description,
            ProposedActions = orderedSteps,
            AffectedComponents = components,
            ExpectedImpact = impact,
            SimulationScore = null,
            DeterminismScore = null,
            RegressionRiskScore = null,
            ApprovalStatus = ApprovalStatus.PendingReview,
            CreatedUtc = createdUtc,
        };
    }

    private static CandidateChangeSet BuildStepSliceChangeSet(
        ProductLearningImprovementPlanRecord plan,
        CandidateChangeSetStep step,
        IReadOnlyList<ChangeSetAffectedComponent> components,
        ExpectedImpact impact,
        DateTime createdUtc)
    {
        string description = BuildStepSliceDescription(plan, step);

        return new CandidateChangeSet
        {
            ChangeSetId = CandidateChangeSetDeterministicIds.StepSliceChangeSetId(plan.PlanId, step.Ordinal),
            SourcePlanId = plan.PlanId,
            Description = description,
            ProposedActions = [step],
            AffectedComponents = components,
            ExpectedImpact = impact,
            SimulationScore = null,
            DeterminismScore = null,
            RegressionRiskScore = null,
            ApprovalStatus = ApprovalStatus.PendingReview,
            CreatedUtc = createdUtc,
        };
    }

    private static string BuildAggregateDescription(ProductLearningImprovementPlanRecord plan)
    {
        if (!string.IsNullOrWhiteSpace(plan.Summary))
        {
            return plan.Summary.Trim();
        }

        return plan.Title.Trim();
    }

    private static string BuildStepSliceDescription(
        ProductLearningImprovementPlanRecord plan,
        CandidateChangeSetStep step)
    {
        string title = plan.Title.Trim();

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} — step {1} ({2}): {3}",
            title,
            step.Ordinal,
            step.ActionType,
            step.Description);
    }
}
