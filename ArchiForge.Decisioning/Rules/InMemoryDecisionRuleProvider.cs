using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Rules;

public class InMemoryDecisionRuleProvider : IDecisionRuleProvider
{
    public Task<DecisionRuleSet> GetRuleSetAsync(CancellationToken ct)
    {
        var ruleSet = new DecisionRuleSet
        {
            RuleSetId = "in-memory",
            Version = "1",
            Rules =
            [
                new DecisionRule
                {
                    Name = "Promote requirement findings",
                    Priority = 100,
                    IsMandatory = true,
                    AppliesToFindingType = "RequirementFinding",
                    Action = "require"
                },

                new DecisionRule
                {
                    Name = "Warn on topology gaps",
                    Priority = 90,
                    IsMandatory = false,
                    AppliesToFindingType = "TopologyGap",
                    Action = "allow"
                },

                new DecisionRule
                {
                    Name = "Topology category coverage",
                    Priority = 89,
                    IsMandatory = false,
                    AppliesToFindingType = "TopologyCoverageFinding",
                    Action = "allow"
                },

                new DecisionRule
                {
                    Name = "Track security control findings",
                    Priority = 88,
                    IsMandatory = false,
                    AppliesToFindingType = "SecurityControlFinding",
                    Action = "allow"
                },

                new DecisionRule
                {
                    Name = "Record policy applicability findings",
                    Priority = 87,
                    IsMandatory = false,
                    AppliesToFindingType = "PolicyApplicabilityFinding",
                    Action = "allow"
                },

                new DecisionRule
                {
                    Name = "Security graph coverage",
                    Priority = 86,
                    IsMandatory = false,
                    AppliesToFindingType = "SecurityCoverageFinding",
                    Action = "allow"
                },

                new DecisionRule
                {
                    Name = "Policy graph coverage",
                    Priority = 85,
                    IsMandatory = false,
                    AppliesToFindingType = "PolicyCoverageFinding",
                    Action = "allow"
                },

                new DecisionRule
                {
                    Name = "Requirement graph coverage",
                    Priority = 84,
                    IsMandatory = false,
                    AppliesToFindingType = "RequirementCoverageFinding",
                    Action = "allow"
                },

                new DecisionRule
                {
                    Name = "Record compliance rule pack findings",
                    Priority = 81,
                    IsMandatory = false,
                    AppliesToFindingType = "ComplianceFinding",
                    Action = "allow"
                },

                new DecisionRule
                {
                    Name = "Prefer cost constraint findings",
                    Priority = 82,
                    IsMandatory = false,
                    AppliesToFindingType = "CostConstraintFinding",
                    Action = "prefer"
                }
            ]
        };

        ruleSet.ComputeHash();
        return Task.FromResult(ruleSet);
    }
}

