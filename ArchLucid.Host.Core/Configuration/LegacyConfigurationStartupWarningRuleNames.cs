namespace ArchLucid.Host.Core.Configuration;

/// <summary>Stable <c>rule_name</c> for TB-002 when obsolete configuration keys remain present.</summary>
public static class LegacyConfigurationStartupWarningRuleNames
{
    public const string IgnoredLegacyKeysPresent = "legacy_ignored_configuration_keys_present";
}
