using System.Text.Json.Serialization;

using ArchiForge.Decisioning.Compliance.Models;

namespace ArchiForge.Decisioning.Governance.PolicyPacks;

/// <summary>
/// Declarative governance payload stored in <see cref="PolicyPackVersion.ContentJson"/> and produced as the merged output of
/// <see cref="Resolution.IEffectiveGovernanceResolver"/>.
/// </summary>
/// <remarks>
/// JSON property names are camelCase via attributes. Dictionary keys use ordinal-ignore-case comparers in memory.
/// Consumed by <see cref="PolicyPackGovernanceFilter"/>, <see cref="ComplianceRulePackGovernanceFilter"/>, advisory scans, and alert evaluators.
/// </remarks>
public class PolicyPackContentDocument
{
    /// <summary>Compliance rules selected by persisted <see cref="Compliance.Models.ComplianceRule"/> id (GUID).</summary>
    [JsonPropertyName("complianceRuleIds")]
    public List<Guid> ComplianceRuleIds { get; set; } = [];

    /// <summary>
    /// String rule IDs matching <see cref="ComplianceRule.RuleId"/> in file-based rule packs (e.g. <c>network-must-have-security-baseline</c>).
    /// </summary>
    [JsonPropertyName("complianceRuleKeys")]
    public List<string> ComplianceRuleKeys { get; set; } = [];

    /// <summary>When non-empty, <see cref="PolicyPackGovernanceFilter.FilterAlertRules"/> restricts evaluation to these simple alert rules.</summary>
    [JsonPropertyName("alertRuleIds")]
    public List<Guid> AlertRuleIds { get; set; } = [];

    /// <summary>When non-empty, restricts composite alert evaluation to these rule ids.</summary>
    [JsonPropertyName("compositeAlertRuleIds")]
    public List<Guid> CompositeAlertRuleIds { get; set; } = [];

    /// <summary>Key/value defaults merged into advisory improvement plans (string values).</summary>
    [JsonPropertyName("advisoryDefaults")]
    public Dictionary<string, string> AdvisoryDefaults { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Opaque key/value metadata merged with precedence during resolution (operator / integration hooks).</summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
