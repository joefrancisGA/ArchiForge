namespace ArchiForge.Host.Core.Configuration;

/// <summary>
/// Merges <c>ArchLucid*</c> configuration over legacy <c>ArchiForge*</c> keys during the product rename.
/// Sunset: remove fallbacks in Phase 7 per <c>docs/ARCHLUCID_RENAME_CHECKLIST.md</c>.
/// </summary>
public static class ArchiForgeConfigurationBridge
{
    public const string ArchLucidSectionName = "ArchLucid";

    public const string ArchLucidAuthSectionName = "ArchLucidAuth";

    public const string LegacyAuthSectionName = "ArchiForgeAuth";

    /// <summary>Effective storage mode: <c>ArchLucid:StorageProvider</c> wins when set.</summary>
    public static ArchiForgeOptions ResolveArchiForgeOptions(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        ArchiForgeOptions options =
            configuration.GetSection(ArchiForgeOptions.SectionName).Get<ArchiForgeOptions>() ?? new ArchiForgeOptions();

        string? lucidStorage = configuration[$"{ArchLucidSectionName}:StorageProvider"]?.Trim();

        if (!string.IsNullOrEmpty(lucidStorage))
        {
            options.StorageProvider = lucidStorage;
        }

        return options;
    }

    /// <summary>Auth setting with <c>ArchLucidAuth:*</c> overriding <c>ArchiForgeAuth:*</c>.</summary>
    public static string? ResolveAuthConfigurationValue(IConfiguration configuration, string relativeKey)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string? lucid = configuration[$"{ArchLucidAuthSectionName}:{relativeKey}"]?.Trim();

        if (!string.IsNullOrEmpty(lucid))
        {
            return lucid;
        }

        return configuration[$"{LegacyAuthSectionName}:{relativeKey}"]?.Trim();
    }
}
