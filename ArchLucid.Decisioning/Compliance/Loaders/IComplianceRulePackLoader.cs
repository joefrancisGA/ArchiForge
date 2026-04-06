using ArchiForge.Decisioning.Compliance.Models;

namespace ArchiForge.Decisioning.Compliance.Loaders;

public interface IComplianceRulePackLoader
{
    Task<ComplianceRulePack> LoadAsync(CancellationToken ct);
}
