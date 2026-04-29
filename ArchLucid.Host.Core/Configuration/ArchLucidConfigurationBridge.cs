using ArchLucid.Persistence.Data.Infrastructure;

namespace ArchLucid.Host.Core.Configuration;

/// <summary>
/// Resolves ArchLucid-first configuration (connection string, product section, auth section).
/// </summary>
public static class ArchLucidConfigurationBridge
{
    public const string ArchLucidSectionName = "ArchLucid";

    public const string ArchLucidAuthSectionName = "ArchLucidAuth";

    public const string PrimarySqlConnectionName = "ArchLucid";

    /// <summary>
    /// SQL connection string: <c>ConnectionStrings:ArchLucid</c> only, normalized with mandatory TLS to SQL Server
    /// (<see cref="SqlConnectionStringSecurity.EnsureSqlClientEncryptMandatory" />).
    /// </summary>
    public static string? ResolveSqlConnectionString(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string? raw = configuration.GetConnectionString(PrimarySqlConnectionName);

        if (raw is null)
            return null;

        return SqlConnectionStringSecurity.EnsureSqlClientEncryptMandatory(raw);
    }

    /// <summary>
    /// Effective product options from <c>ArchLucid</c> section and flat <c>ArchLucid:StorageProvider</c>.
    /// </summary>
    public static ArchLucidOptions ResolveArchLucidOptions(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        ArchLucidOptions options =
            configuration.GetSection(ArchLucidSectionName).Get<ArchLucidOptions>() ?? new ArchLucidOptions();

        string? lucidStorage = configuration[$"{ArchLucidSectionName}:StorageProvider"]?.Trim();

        if (!string.IsNullOrEmpty(lucidStorage))

            options.StorageProvider = lucidStorage;


        return options;
    }

    /// <summary>Auth setting from <c>ArchLucidAuth:*</c> only.</summary>
    public static string? ResolveAuthConfigurationValue(IConfiguration configuration, string relativeKey)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return configuration[$"{ArchLucidAuthSectionName}:{relativeKey}"]?.Trim();
    }
}
