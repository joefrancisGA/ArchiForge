namespace ArchiForge.Api.Health;

/// <summary>Paths under <see cref="AppContext.BaseDirectory"/> for bundled policy/content files.</summary>
public static class EmbeddedContentPaths
{
    /// <summary>Default compliance rule pack loaded at startup by <c>RegisterDecisioningEngines</c>.</summary>
    public const string ComplianceRulePackRelativePath = "Compliance/RulePacks/default-compliance.rules.json";
}
