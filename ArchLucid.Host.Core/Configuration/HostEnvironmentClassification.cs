namespace ArchLucid.Host.Core.Configuration;

/// <summary>
/// Shared classification for environments that should use production-style security defaults
/// (Content Safety, RLS break-glass alerts, etc.).
/// </summary>
public static class HostEnvironmentClassification
{
    /// <summary>
    /// True when <see cref="IHostEnvironment.IsProduction"/> or <see cref="IHostEnvironment.IsStaging"/>,
    /// or when <c>ARCHLUCID_ENVIRONMENT</c> (configuration then process environment) is <c>Production</c> or <c>Staging</c>.
    /// </summary>
    public static bool IsProductionOrStagingLike(IHostEnvironment hostEnvironment, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(hostEnvironment);
        ArgumentNullException.ThrowIfNull(configuration);

        if (hostEnvironment.IsProduction() || hostEnvironment.IsStaging())
            return true;

        string? archLucidEnv = configuration["ARCHLUCID_ENVIRONMENT"];

        if (string.IsNullOrWhiteSpace(archLucidEnv))
            archLucidEnv = Environment.GetEnvironmentVariable("ARCHLUCID_ENVIRONMENT");

        if (string.IsNullOrWhiteSpace(archLucidEnv))
            return false;

        string trimmed = archLucidEnv.Trim();

        return string.Equals(trimmed, "Production", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(trimmed, "Staging", StringComparison.OrdinalIgnoreCase);
    }
}
