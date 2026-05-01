using ArchLucid.Decisioning.Compliance.Evaluators;
using ArchLucid.Decisioning.Compliance.Loaders;
using ArchLucid.Decisioning.Compliance.Models;
using ArchLucid.Decisioning.Findings;
using ArchLucid.Decisioning.Findings.Payloads;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Models;
using ArchLucid.KnowledgeGraph.Models;

namespace ArchLucid.Decisioning.Services;

/// <summary>
///     <see cref="IFindingEngine" /> that evaluates a <see cref="GraphSnapshot" /> against a
///     <see cref="ComplianceRulePack" /> and emits <c>ComplianceFinding</c> findings for each
///     detected <see cref="ComplianceViolation" />.
///     Severity is mapped from the violation's string value to <see cref="FindingSeverity" />;
///     unrecognised values default to <see cref="FindingSeverity.Info" />.
/// </summary>
public class ComplianceFindingEngine(
    IComplianceRulePackProvider rulePackProvider,
    IComplianceRulePackValidator packValidator,
    IComplianceEvaluator evaluator) : IFindingEngine
{
    public string EngineType => "compliance";

    public string Category => "Compliance";

    public async Task<IReadOnlyList<Finding>> AnalyzeAsync(
        GraphSnapshot graphSnapshot,
        CancellationToken ct)
    {
        ComplianceRulePack rulePack = await rulePackProvider.GetRulePackAsync(ct);
        packValidator.Validate(rulePack);

        ComplianceEvaluationResult evaluation = evaluator.Evaluate(graphSnapshot, rulePack);
        List<Finding> findings = [];

        foreach (ComplianceViolation violation in evaluation.Violations)

            findings.Add(new Finding
            {
                FindingSchemaVersion = FindingsSchema.CurrentFindingVersion,
                FindingType = "ComplianceFinding",
                Category = Category,
                EngineType = EngineType,
                Severity = ParseSeverity(violation.Severity),
                Title = $"Compliance gap: {violation.ControlName}",
                Rationale = violation.Description,
                RelatedNodeIds = violation.AffectedNodeIds.ToList(),
                RecommendedActions =
                [
                    $"Add or apply {violation.ControlName} to {violation.AppliesToCategory} resources."
                ],
                PayloadType = nameof(ComplianceFindingPayload),
                Payload = new ComplianceFindingPayload
                {
                    RulePackId = rulePack.RulePackId,
                    RulePackVersion = rulePack.Version,
                    RuleId = violation.RuleId,
                    ControlId = violation.ControlId,
                    ControlName = violation.ControlName,
                    AppliesToCategory = violation.AppliesToCategory,
                    AffectedResources = violation.AffectedResources.ToList()
                },
                Trace = new ExplainabilityTrace
                {
                    RulesApplied = [violation.RuleId],
                    GraphNodeIdsExamined = violation.AffectedNodeIds.ToList(),
                    DecisionsTaken =
                    [
                        "Detected uncovered resources against compliance rule requirements."
                    ],
                    AlternativePathsConsidered = [ExplainabilityTraceMarkers.RuleBasedDeterministicSinglePathNote],
                    Notes = [$"Rule pack: {rulePack.RulePackId} v{rulePack.Version}"]
                }
            });

        return findings;
    }

    private static FindingSeverity ParseSeverity(string severity)
    {
        return severity.ToLowerInvariant() switch
        {
            "critical" => FindingSeverity.Critical,
            "error" => FindingSeverity.Error,
            "warning" => FindingSeverity.Warning,
            _ => FindingSeverity.Info
        };
    }
}
