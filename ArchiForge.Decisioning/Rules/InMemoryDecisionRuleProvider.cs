using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Rules;

/// <summary>
/// A hard-coded, in-memory <see cref="IDecisionRuleProvider"/> used for local development,
/// integration tests, and demo environments.
/// </summary>
/// <remarks>
/// This provider always returns the same static rule set and is not suitable for production
/// environments that require dynamic or tenant-specific rule configuration.
/// Replace it with a database-backed provider via DI when persistent rule management is needed.
/// </remarks>
public class InMemoryDecisionRuleProvider : IDecisionRuleProvider
{
    // ── Rule-set identity ───────────────────────────────────────────────────

    private const string RuleSetId = "in-memory";
    private const string RuleSetVersion = "1";

    // ── Finding types ───────────────────────────────────────────────────────

    private const string FindingTypeRequirement = "RequirementFinding";
    private const string FindingTypeTopologyGap = "TopologyGap";
    private const string FindingTypeTopologyCoverage = "TopologyCoverageFinding";
    private const string FindingTypeSecurityControl = "SecurityControlFinding";
    private const string FindingTypePolicyApplicability = "PolicyApplicabilityFinding";
    private const string FindingTypeSecurityCoverage = "SecurityCoverageFinding";
    private const string FindingTypePolicyCoverage = "PolicyCoverageFinding";
    private const string FindingTypeRequirementCoverage = "RequirementCoverageFinding";
    private const string FindingTypeCompliance = "ComplianceFinding";
    private const string FindingTypeCostConstraint = "CostConstraintFinding";

    // ── Rule actions ────────────────────────────────────────────────────────

    private const string ActionRequire = "require";
    private const string ActionAllow = "allow";
    private const string ActionPrefer = "prefer";

    public Task<DecisionRuleSet> GetRuleSetAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ruleSet = new DecisionRuleSet
        {
            RuleSetId = RuleSetId,
            Version = RuleSetVersion,
            Rules =
            [
                new DecisionRule
                {
                    Name = "Promote requirement findings",
                    Priority = 100,
                    IsMandatory = true,
                    AppliesToFindingType = FindingTypeRequirement,
                    Action = ActionRequire
                },

                new DecisionRule
                {
                    Name = "Warn on topology gaps",
                    Priority = 90,
                    IsMandatory = false,
                    AppliesToFindingType = FindingTypeTopologyGap,
                    Action = ActionAllow
                },

                new DecisionRule
                {
                    Name = "Topology category coverage",
                    Priority = 89,
                    IsMandatory = false,
                    AppliesToFindingType = FindingTypeTopologyCoverage,
                    Action = ActionAllow
                },

                new DecisionRule
                {
                    Name = "Track security control findings",
                    Priority = 88,
                    IsMandatory = false,
                    AppliesToFindingType = FindingTypeSecurityControl,
                    Action = ActionAllow
                },

                new DecisionRule
                {
                    Name = "Record policy applicability findings",
                    Priority = 87,
                    IsMandatory = false,
                    AppliesToFindingType = FindingTypePolicyApplicability,
                    Action = ActionAllow
                },

                new DecisionRule
                {
                    Name = "Security graph coverage",
                    Priority = 86,
                    IsMandatory = false,
                    AppliesToFindingType = FindingTypeSecurityCoverage,
                    Action = ActionAllow
                },

                new DecisionRule
                {
                    Name = "Policy graph coverage",
                    Priority = 85,
                    IsMandatory = false,
                    AppliesToFindingType = FindingTypePolicyCoverage,
                    Action = ActionAllow
                },

                new DecisionRule
                {
                    Name = "Requirement graph coverage",
                    Priority = 84,
                    IsMandatory = false,
                    AppliesToFindingType = FindingTypeRequirementCoverage,
                    Action = ActionAllow
                },

                new DecisionRule
                {
                    Name = "Record compliance rule pack findings",
                    Priority = 81,
                    IsMandatory = false,
                    AppliesToFindingType = FindingTypeCompliance,
                    Action = ActionAllow
                },

                new DecisionRule
                {
                    Name = "Prefer cost constraint findings",
                    Priority = 82,
                    IsMandatory = false,
                    AppliesToFindingType = FindingTypeCostConstraint,
                    Action = ActionPrefer
                }
            ]
        };

        ruleSet.ComputeHash();
        return Task.FromResult(ruleSet);
    }
}
