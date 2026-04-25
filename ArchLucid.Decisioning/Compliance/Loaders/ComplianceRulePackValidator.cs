using ArchLucid.Decisioning.Compliance.Models;

namespace ArchLucid.Decisioning.Compliance.Loaders;

public class ComplianceRulePackValidator : IComplianceRulePackValidator
{
    public void Validate(ComplianceRulePack rulePack)
    {
        ArgumentNullException.ThrowIfNull(rulePack);

        if (string.IsNullOrWhiteSpace(rulePack.RulePackId))
            throw new InvalidOperationException("Compliance RulePackId is required.");

        if (string.IsNullOrWhiteSpace(rulePack.Version))
            throw new InvalidOperationException("Compliance rule pack version is required.");

        List<string> duplicateIds = rulePack.Rules
            .GroupBy(x => x.RuleId, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        if (duplicateIds.Count > 0)
            throw new InvalidOperationException(
                $"Duplicate compliance rule IDs found: {string.Join(", ", duplicateIds)}");

        foreach (ComplianceRule rule in rulePack.Rules)

            if (string.IsNullOrWhiteSpace(rule.RuleId))
                throw new InvalidOperationException("Each compliance rule must have a RuleId.");
    }
}
