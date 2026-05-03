using ArchLucid.Core.Diagnostics;

namespace ArchLucid.Host.Core.Configuration;

/// <summary>
/// Logs when configuration still uses the pre-rename product section or connection-string name
/// (for example Key Vault secret mappings) so operators know those keys are ignored.
/// </summary>
public static class ArchLucidLegacyConfigurationWarnings
{
    /// <summary>
    /// Earliest calendar date (ISO 8601) after which startup may fail if legacy keys are still present.
    /// Until then behavior is log-warning only; confirm with <c>docs/CONFIG_BRIDGE_SUNSET.md</c> before enforcing.
    /// </summary>
    public const string LegacyConfigurationKeysHardEnforcementNoEarlierThan = "2027-07-01";

    private static readonly string LegacyConnectionStringName = "Archi" + "Forge";

    private static readonly string LegacyProductSection = "Archi" + "Forge";

    private static readonly string LegacyAuthSection = "Archi" + "Forge" + "Auth";

    /// <summary>
    /// Emits a single warning if any ignored legacy keys are present.
    /// </summary>
    public static void LogIfLegacyKeysPresent(IConfiguration configuration, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);

        List<string> found = [];

        if (!string.IsNullOrWhiteSpace(configuration.GetConnectionString(LegacyConnectionStringName)))

            found.Add("ConnectionStrings:" + LegacyConnectionStringName);

        if (configuration.GetSection(LegacyProductSection).GetChildren().Any())

            found.Add(LegacyProductSection + ":*");

        if (configuration.GetSection(LegacyAuthSection).GetChildren().Any())

            found.Add(LegacyAuthSection + ":*");

        if (found.Count == 0)
            return;

        logger.LogWarning(
            "Legacy configuration keys are present but ignored: {LegacyKeys}. Use ConnectionStrings:ArchLucid, ArchLucid:*, and ArchLucidAuth:* only. Hard enforcement is not scheduled before {SunsetDate}; see docs/CONFIG_BRIDGE_SUNSET.md.",
            string.Join(", ", found),
            LegacyConfigurationKeysHardEnforcementNoEarlierThan);

        ArchLucidInstrumentation.RecordStartupConfigWarning(LegacyConfigurationStartupWarningRuleNames.IgnoredLegacyKeysPresent);
    }
}
