using ArchLucid.Decisioning.Compliance.Models;

namespace ArchLucid.Decisioning.Compliance.Loaders;

public class FileComplianceRulePackProvider(IComplianceRulePackLoader loader) : IComplianceRulePackProvider
{
    public Task<ComplianceRulePack> GetRulePackAsync(CancellationToken ct)
    {
        return loader.LoadAsync(ct);
    }
}
