using ArchLucid.Decisioning.Alerts.Simulation;
// Type aliases avoid importing sibling namespace `...Alerts.Composite` (bare `Composite` → CS0118 here).
using AlertRuleCondition = ArchLucid.Decisioning.Alerts.Composite.AlertRuleCondition;
using CompositeAlertRule = ArchLucid.Decisioning.Alerts.Composite.CompositeAlertRule;

namespace ArchLucid.Decisioning.Alerts.Tuning;

/// <summary>
///     Sweeps <see cref="ThresholdRecommendationRequest.CandidateThresholds" /> via <see cref="IRuleSimulationService" />
///     and ranks results with <see cref="IAlertNoiseScorer" />.
/// </summary>
/// <param name="simulationService">Dry-run evaluator over historical contexts.</param>
/// <param name="noiseScorer">Heuristic ranking of each simulation.</param>
public sealed class ThresholdRecommendationService(
    IRuleSimulationService simulationService,
    IAlertNoiseScorer noiseScorer) : IThresholdRecommendationService
{
    private const string RuleKindSimple = RuleKindConstants.Simple;
    private const string DefaultProjectSlug = "default";
    private const string RuleKindComposite = RuleKindConstants.Composite;

    /// <inheritdoc />
    public async Task<ThresholdRecommendationResult> RecommendAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        ThresholdRecommendationRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        ThresholdRecommendationResult result = new()
        {
            EvaluatedUtc = DateTime.UtcNow, RuleKind = request.RuleKind, TunedMetricType = request.TunedMetricType
        };

        string slug = string.IsNullOrWhiteSpace(request.RunProjectSlug)
            ? DefaultProjectSlug
            : request.RunProjectSlug.Trim();

        foreach (decimal threshold in request.CandidateThresholds.Distinct().OrderBy(x => x))
        {
            RuleSimulationResult? simulation;

            if (request.RuleKind.Equals(RuleKindSimple, StringComparison.OrdinalIgnoreCase) &&
                request.BaseSimpleRule is not null)
            {
                AlertRule baseRule = AlignSimpleRuleMetric(request.BaseSimpleRule, request.TunedMetricType);
                AlertRule candidateRule = CloneSimpleRuleWithThreshold(baseRule, threshold);

                simulation = await simulationService
                        .SimulateAsync(
                            tenantId,
                            workspaceId,
                            projectId,
                            new RuleSimulationRequest
                            {
                                RuleKind = RuleKindSimple,
                                SimpleRule = candidateRule,
                                RecentRunCount = request.RecentRunCount,
                                UseHistoricalWindow = true,
                                RunProjectSlug = slug
                            },
                            ct)
                    ;
            }
            else if (request.RuleKind.Equals(RuleKindComposite, StringComparison.OrdinalIgnoreCase) &&
                     request.BaseCompositeRule is not null)
            {
                CompositeAlertRule candidateRule = CloneCompositeRuleWithThreshold(
                    request.BaseCompositeRule,
                    request.TunedMetricType,
                    threshold);

                simulation = await simulationService
                        .SimulateAsync(
                            tenantId,
                            workspaceId,
                            projectId,
                            new RuleSimulationRequest
                            {
                                RuleKind = RuleKindComposite,
                                CompositeRule = candidateRule,
                                RecentRunCount = request.RecentRunCount,
                                UseHistoricalWindow = true,
                                RunProjectSlug = slug
                            },
                            ct)
                    ;
            }
            else

                continue;


            NoiseScoreBreakdown score = noiseScorer.Score(
                simulation,
                request.TargetCreatedAlertCountMin,
                request.TargetCreatedAlertCountMax);

            result.Candidates.Add(
                new ThresholdCandidateEvaluation
                {
                    Candidate = new ThresholdCandidate
                    {
                        ThresholdValue = threshold, Label = threshold.ToString("0.##")
                    },
                    SimulationResult = simulation,
                    ScoreBreakdown = score
                });
        }

        result.RecommendedCandidate = result.Candidates
            .OrderByDescending(x => x.ScoreBreakdown.FinalScore)
            .ThenByDescending(x => x.SimulationResult.MatchedCount)
            .FirstOrDefault();

        if (result.RecommendedCandidate is not null)
        {
            result.SummaryNotes.Add(
                $"Recommended threshold: {result.RecommendedCandidate.Candidate.ThresholdValue:0.##}");

            result.SummaryNotes.Add(
                "Recommended candidate would create " +
                $"{result.RecommendedCandidate.SimulationResult.WouldCreateCount} alert(s) " +
                $"and suppress {result.RecommendedCandidate.SimulationResult.WouldSuppressCount}.");
        }
        else if (result.Candidates.Count == 0)

            result.SummaryNotes.Add(
                "No candidates were evaluated. Check RuleKind, base rule, and candidate thresholds.");


        result.SummaryNotes.Add($"Evaluated {result.Candidates.Count} candidate threshold(s).");

        return result;
    }

    private static AlertRule AlignSimpleRuleMetric(AlertRule source, string tunedMetricType)
    {
        AlertRule copy = CloneSimpleRuleWithThreshold(source, source.ThresholdValue);
        if (!string.IsNullOrWhiteSpace(tunedMetricType))
            copy.RuleType = tunedMetricType.Trim();
        return copy;
    }

    private static AlertRule CloneSimpleRuleWithThreshold(AlertRule source, decimal threshold)
    {
        return new AlertRule
        {
            RuleId = source.RuleId,
            TenantId = source.TenantId,
            WorkspaceId = source.WorkspaceId,
            ProjectId = source.ProjectId,
            Name = source.Name,
            RuleType = source.RuleType,
            Severity = source.Severity,
            ThresholdValue = threshold,
            IsEnabled = source.IsEnabled,
            TargetChannelType = source.TargetChannelType,
            MetadataJson = source.MetadataJson,
            CreatedUtc = source.CreatedUtc
        };
    }

    private static CompositeAlertRule CloneCompositeRuleWithThreshold(
        CompositeAlertRule source,
        string tunedMetricType,
        decimal threshold)
    {
        return new CompositeAlertRule
        {
            CompositeRuleId = source.CompositeRuleId,
            TenantId = source.TenantId,
            WorkspaceId = source.WorkspaceId,
            ProjectId = source.ProjectId,
            Name = source.Name,
            Severity = source.Severity,
            Operator = source.Operator,
            IsEnabled = source.IsEnabled,
            SuppressionWindowMinutes = source.SuppressionWindowMinutes,
            CooldownMinutes = source.CooldownMinutes,
            ReopenDeltaThreshold = source.ReopenDeltaThreshold,
            DedupeScope = source.DedupeScope,
            TargetChannelType = source.TargetChannelType,
            CreatedUtc = source.CreatedUtc,
            Conditions = source.Conditions
                .Select(c => new AlertRuleCondition
                {
                    ConditionId = c.ConditionId,
                    MetricType = c.MetricType,
                    Operator = c.Operator,
                    ThresholdValue = c.MetricType.Equals(tunedMetricType, StringComparison.OrdinalIgnoreCase)
                        ? threshold
                        : c.ThresholdValue
                })
                .ToList()
        };
    }
}
