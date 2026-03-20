using ArchiForge.Decisioning.Compliance.Models;

namespace ArchiForge.Decisioning.Compliance.Loaders;

public interface IComplianceRulePackValidator
{
    void Validate(ComplianceRulePack rulePack);
}
