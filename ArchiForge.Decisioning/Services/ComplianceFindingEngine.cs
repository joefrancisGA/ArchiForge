using ArchiForge.Decisioning.Compliance.Evaluators;
using ArchiForge.Decisioning.Compliance.Loaders;
using ArchiForge.Decisioning.Compliance.Models;
using ArchiForge.Decisioning.Findings.Payloads;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.Decisioning.Services;

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
        List<Finding> findings = new();

        foreach (ComplianceViolation violation in evaluation.Violations)
        {
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
                    ]
                }
            });
        }

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
