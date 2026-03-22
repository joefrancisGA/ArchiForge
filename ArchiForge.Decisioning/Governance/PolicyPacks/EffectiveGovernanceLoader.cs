using System.Text.Json;

namespace ArchiForge.Decisioning.Governance.PolicyPacks;

public sealed class EffectiveGovernanceLoader(IPolicyPackResolver resolver) : IEffectiveGovernanceLoader
{
    public async Task<PolicyPackContentDocument> LoadEffectiveContentAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        var set = await resolver.ResolveAsync(tenantId, workspaceId, projectId, ct).ConfigureAwait(false);

        var merged = new PolicyPackContentDocument();

        foreach (var pack in set.Packs)
        {
            PolicyPackContentDocument? doc;
            try
            {
                doc = JsonSerializer.Deserialize<PolicyPackContentDocument>(
                    pack.ContentJson,
                    PolicyPackJsonSerializerOptions.Default);
            }
            catch (JsonException)
            {
                continue;
            }

            if (doc is null)
                continue;

            merged.ComplianceRuleIds.AddRange(doc.ComplianceRuleIds);
            merged.ComplianceRuleKeys.AddRange(doc.ComplianceRuleKeys);
            merged.AlertRuleIds.AddRange(doc.AlertRuleIds);
            merged.CompositeAlertRuleIds.AddRange(doc.CompositeAlertRuleIds);

            foreach (var kvp in doc.AdvisoryDefaults)
                merged.AdvisoryDefaults[kvp.Key] = kvp.Value;

            foreach (var kvp in doc.Metadata)
                merged.Metadata[kvp.Key] = kvp.Value;
        }

        merged.ComplianceRuleIds = merged.ComplianceRuleIds.Distinct().ToList();
        merged.ComplianceRuleKeys = merged.ComplianceRuleKeys.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        merged.AlertRuleIds = merged.AlertRuleIds.Distinct().ToList();
        merged.CompositeAlertRuleIds = merged.CompositeAlertRuleIds.Distinct().ToList();

        return merged;
    }
}
