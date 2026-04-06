using ArchiForge.Decisioning.Compliance.Models;

namespace ArchiForge.Decisioning.Compliance.Loaders;

public interface IComplianceRulePackProvider
{
    Task<ComplianceRulePack> GetRulePackAsync(CancellationToken ct);
}
