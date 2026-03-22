using System.Text.Json.Serialization;

namespace ArchiForge.Decisioning.Governance.PolicyPacks;

/// <summary>
/// Declarative payload merged from effective policy packs (JSON-serializable).
/// </summary>
public class PolicyPackContentDocument
{
    [JsonPropertyName("complianceRuleIds")]
    public List<Guid> ComplianceRuleIds { get; set; } = [];

    /// <summary>String rule IDs matching <see cref="Compliance.Models.ComplianceRule.RuleId"/> in file-based rule packs (e.g. <c>network-must-have-security-baseline</c>).</summary>
    [JsonPropertyName("complianceRuleKeys")]
    public List<string> ComplianceRuleKeys { get; set; } = [];

    [JsonPropertyName("alertRuleIds")]
    public List<Guid> AlertRuleIds { get; set; } = [];

    [JsonPropertyName("compositeAlertRuleIds")]
    public List<Guid> CompositeAlertRuleIds { get; set; } = [];

    [JsonPropertyName("advisoryDefaults")]
    public Dictionary<string, string> AdvisoryDefaults { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
