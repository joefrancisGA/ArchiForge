using ArchiForge.Decisioning.Alerts;
using ArchiForge.Decisioning.Alerts.Composite;
using ArchiForge.Decisioning.Alerts.Simulation;

namespace ArchiForge.Persistence.Alerts.Simulation;

/// <summary>
/// Default <see cref="IRuleSimulationService"/>: replays rules against contexts from <see cref="IAlertSimulationContextProvider"/> without persisting simple-rule alerts; composite path uses live suppression reads.
/// </summary>
/// <param name="alertEvaluator">Production simple rule evaluation.</param>
/// <param name="metricSnapshotBuilder">Builds metrics for composite predicates.</param>
/// <param name="compositeEvaluator">Composite AND/OR evaluation.</param>
/// <param name="suppressionPolicy">Same policy as production (queries open alerts for dedupe).</param>
/// <param name="contextProvider">Builds <see cref="AlertEvaluationContext"/> per run.</param>
public sealed class RuleSimulationService(
    IAlertEvaluator alertEvaluator,
    IAlertMetricSnapshotBuilder metricSnapshotBuilder,
    ICompositeAlertRuleEvaluator compositeEvaluator,
    IAlertSuppressionPolicy suppressionPolicy,
    IAlertSimulationContextProvider contextProvider) : IRuleSimulationService
{
    /// <inheritdoc />
    public async Task<RuleSimulationResult> SimulateAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        RuleSimulationRequest request,
        CancellationToken ct)
    {
        if (request is { UseHistoricalWindow: false, RunId: null })
        {
            return new RuleSimulationResult
            {
                RuleKind = request.RuleKind,
                SimulatedUtc = DateTime.UtcNow,
                EvaluatedRunCount = 0,
                SummaryNotes = { "UseHistoricalWindow is false and no RunId was provided; nothing evaluated." },
            };
        }

        var contexts = await contextProvider
            .GetContextsAsync(
                tenantId,
                workspaceId,
                projectId,
                request.RunId,
                request.ComparedToRunId,
                request.RecentRunCount,
                request.RunProjectSlug,
                ct)
            .ConfigureAwait(false);

        var result = new RuleSimulationResult
        {
            RuleKind = request.RuleKind,
            SimulatedUtc = DateTime.UtcNow,
            EvaluatedRunCount = contexts.Count,
        };

        if (contexts.Count == 0)
        {
            result.SummaryNotes.Add("No evaluation contexts were built (no runs or missing golden manifests).");
            return result;
        }

        foreach (var context in contexts)
        {
            if (request.RuleKind.Equals("Simple", StringComparison.OrdinalIgnoreCase) &&
                request.SimpleRule is not null)
            {
                var rule = CloneSimpleForSimulation(request.SimpleRule);
                var generated = alertEvaluator.Evaluate([rule], context);

                if (generated.Count > 0)
                {
                    foreach (var alert in generated)
                    {
                        result.Outcomes.Add(
                            new SimulatedAlertOutcome
                            {
                                RunId = context.RunId,
                                ComparedToRunId = context.ComparedToRunId,
                                RuleMatched = true,
                                WouldCreateAlert = true,
                                WouldBeSuppressed = false,
                                Title = alert.Title,
                                Severity = alert.Severity,
                                Description = alert.Description,
                                DeduplicationKey = alert.DeduplicationKey,
                                SuppressionReason = "No suppression logic applied for simple rule dry-run.",
                                EvaluationMode = "Simple",
                                Notes = ["Simple rule matched (production evaluator; no persistence or delivery)."],
                            });
                    }
                }
                else
                {
                    result.Outcomes.Add(
                        new SimulatedAlertOutcome
                        {
                            RunId = context.RunId,
                            ComparedToRunId = context.ComparedToRunId,
                            RuleMatched = false,
                            WouldCreateAlert = false,
                            WouldBeSuppressed = false,
                            Title = request.SimpleRule.Name,
                            Severity = request.SimpleRule.Severity,
                            Description = "Rule did not match.",
                            DeduplicationKey = string.Empty,
                            SuppressionReason = string.Empty,
                            EvaluationMode = "Simple",
                            Notes = ["Simple rule did not match."],
                        });
                }
            }
            else if (request.RuleKind.Equals("Composite", StringComparison.OrdinalIgnoreCase) &&
                     request.CompositeRule is not null)
            {
                var compositeRule = CloneCompositeForSimulation(request.CompositeRule);
                var snapshot = metricSnapshotBuilder.Build(context);
                var matched = compositeEvaluator.Evaluate(compositeRule, snapshot);

                if (!matched)
                {
                    result.Outcomes.Add(
                        new SimulatedAlertOutcome
                        {
                            RunId = context.RunId,
                            ComparedToRunId = context.ComparedToRunId,
                            RuleMatched = false,
                            WouldCreateAlert = false,
                            WouldBeSuppressed = false,
                            Title = request.CompositeRule.Name,
                            Severity = request.CompositeRule.Severity,
                            Description = "Composite rule did not match.",
                            DeduplicationKey = string.Empty,
                            SuppressionReason = string.Empty,
                            EvaluationMode = "Composite",
                            Notes = ["Composite rule did not match current metric snapshot."],
                        });
                    continue;
                }

                var suppression = await suppressionPolicy
                    .DecideAsync(compositeRule, context, snapshot, ct)
                    .ConfigureAwait(false);

                result.Outcomes.Add(
                    new SimulatedAlertOutcome
                    {
                        RunId = context.RunId,
                        ComparedToRunId = context.ComparedToRunId,
                        RuleMatched = true,
                        WouldCreateAlert = suppression.ShouldCreateAlert,
                        WouldBeSuppressed = !suppression.ShouldCreateAlert,
                        Title = $"Composite alert: {request.CompositeRule.Name}",
                        Severity = request.CompositeRule.Severity,
                        Description = suppression.Reason,
                        DeduplicationKey = suppression.DeduplicationKey,
                        SuppressionReason = suppression.Reason,
                        EvaluationMode = "Composite",
                        Notes =
                        [
                            "Composite rule matched; suppression uses live alert store (read-only for simulation).",
                        ],
                    });
            }
        }

        result.MatchedCount = result.Outcomes.Count(x => x.RuleMatched);
        result.WouldCreateCount = result.Outcomes.Count(x => x.WouldCreateAlert);
        result.WouldSuppressCount = result.Outcomes.Count(x => x.WouldBeSuppressed);

        result.SummaryNotes.Add($"Evaluated {result.EvaluatedRunCount} run context(s).");
        result.SummaryNotes.Add($"{result.MatchedCount} outcome(s) matched.");
        result.SummaryNotes.Add($"{result.WouldCreateCount} would create alert(s).");
        result.SummaryNotes.Add($"{result.WouldSuppressCount} would be suppressed.");

        return result;
    }

    /// <inheritdoc />
    public async Task<RuleCandidateComparisonResult> CompareCandidatesAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        RuleCandidateComparisonRequest request,
        CancellationToken ct)
    {
        RuleSimulationResult candidateA;
        RuleSimulationResult candidateB;

        if (request.RuleKind.Equals("Simple", StringComparison.OrdinalIgnoreCase))
        {
            candidateA = await SimulateAsync(
                    tenantId,
                    workspaceId,
                    projectId,
                    new RuleSimulationRequest
                    {
                        RuleKind = "Simple",
                        SimpleRule = request.CandidateASimpleRule,
                        RecentRunCount = request.RecentRunCount,
                        RunProjectSlug = request.RunProjectSlug,
                    },
                    ct)
                .ConfigureAwait(false);

            candidateB = await SimulateAsync(
                    tenantId,
                    workspaceId,
                    projectId,
                    new RuleSimulationRequest
                    {
                        RuleKind = "Simple",
                        SimpleRule = request.CandidateBSimpleRule,
                        RecentRunCount = request.RecentRunCount,
                        RunProjectSlug = request.RunProjectSlug,
                    },
                    ct)
                .ConfigureAwait(false);
        }
        else
        {
            candidateA = await SimulateAsync(
                    tenantId,
                    workspaceId,
                    projectId,
                    new RuleSimulationRequest
                    {
                        RuleKind = "Composite",
                        CompositeRule = request.CandidateACompositeRule,
                        RecentRunCount = request.RecentRunCount,
                        RunProjectSlug = request.RunProjectSlug,
                    },
                    ct)
                .ConfigureAwait(false);

            candidateB = await SimulateAsync(
                    tenantId,
                    workspaceId,
                    projectId,
                    new RuleSimulationRequest
                    {
                        RuleKind = "Composite",
                        CompositeRule = request.CandidateBCompositeRule,
                        RecentRunCount = request.RecentRunCount,
                        RunProjectSlug = request.RunProjectSlug,
                    },
                    ct)
                .ConfigureAwait(false);
        }

        var result = new RuleCandidateComparisonResult
        {
            CandidateA = candidateA,
            CandidateB = candidateB,
        };

        result.SummaryNotes.Add($"Candidate A would create {candidateA.WouldCreateCount} alert(s).");
        result.SummaryNotes.Add($"Candidate B would create {candidateB.WouldCreateCount} alert(s).");
        result.SummaryNotes.Add($"Candidate A would suppress {candidateA.WouldSuppressCount} outcome(s).");
        result.SummaryNotes.Add($"Candidate B would suppress {candidateB.WouldSuppressCount} outcome(s).");

        return result;
    }

    private static AlertRule CloneSimpleForSimulation(AlertRule r) =>
        new()
        {
            RuleId = r.RuleId == Guid.Empty ? Guid.NewGuid() : r.RuleId,
            TenantId = r.TenantId,
            WorkspaceId = r.WorkspaceId,
            ProjectId = r.ProjectId,
            Name = r.Name,
            RuleType = r.RuleType,
            Severity = r.Severity,
            ThresholdValue = r.ThresholdValue,
            IsEnabled = true,
            TargetChannelType = r.TargetChannelType,
            MetadataJson = r.MetadataJson,
            CreatedUtc = r.CreatedUtc,
        };

    private static CompositeAlertRule CloneCompositeForSimulation(CompositeAlertRule r)
    {
        var id = r.CompositeRuleId == Guid.Empty ? Guid.NewGuid() : r.CompositeRuleId;
        return new CompositeAlertRule
        {
            CompositeRuleId = id,
            TenantId = r.TenantId,
            WorkspaceId = r.WorkspaceId,
            ProjectId = r.ProjectId,
            Name = r.Name,
            Severity = r.Severity,
            Operator = r.Operator,
            IsEnabled = true,
            SuppressionWindowMinutes = r.SuppressionWindowMinutes,
            CooldownMinutes = r.CooldownMinutes,
            ReopenDeltaThreshold = r.ReopenDeltaThreshold,
            DedupeScope = r.DedupeScope,
            TargetChannelType = r.TargetChannelType,
            CreatedUtc = r.CreatedUtc,
            Conditions = r.Conditions
                .Select(
                    c => new AlertRuleCondition
                    {
                        ConditionId = c.ConditionId == Guid.Empty ? Guid.NewGuid() : c.ConditionId,
                        MetricType = c.MetricType,
                        Operator = c.Operator,
                        ThresholdValue = c.ThresholdValue,
                    })
                .ToList(),
        };
    }
}
