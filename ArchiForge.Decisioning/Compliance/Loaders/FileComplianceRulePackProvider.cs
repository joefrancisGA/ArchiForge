using ArchiForge.Decisioning.Compliance.Models;

namespace ArchiForge.Decisioning.Compliance.Loaders;

public class FileComplianceRulePackProvider(IComplianceRulePackLoader loader) : IComplianceRulePackProvider
{
    public Task<ComplianceRulePack> GetRulePackAsync(CancellationToken ct) => loader.LoadAsync(ct);
}
