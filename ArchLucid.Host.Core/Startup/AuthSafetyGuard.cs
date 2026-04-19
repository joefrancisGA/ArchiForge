using ArchLucid.Host.Core.Configuration;

namespace ArchLucid.Host.Core.Startup;

/// <summary>Fail-fast checks for dangerous authentication configuration combinations.</summary>
public static class AuthSafetyGuard
{
    private const string DevelopmentBypassNotAllowedMessage =
        "DevelopmentBypass auth mode is not permitted in Production environments. "
        + "Set ArchLucidAuth:Mode to JwtBearer or ApiKey.";

    private const string DevelopmentBypassAllNotAllowedMessage =
        "Authentication:ApiKey:DevelopmentBypassAll is not permitted in Production environments. "
        + "Set Authentication:ApiKey:DevelopmentBypassAll to false.";

    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> when any production-incompatible development auth
    /// setting is active and the host is treated as production-like: <see cref="IHostEnvironment.IsProduction"/>,
    /// <c>ASPNETCORE_ENVIRONMENT</c> / <c>ARCHLUCID_ENVIRONMENT</c> names containing <c>prod</c> (excluding
    /// <c>non-production</c> / <c>nonproduction</c>), or exact Production via configuration.
    /// </summary>
    /// <remarks>
    /// Checks: <c>ArchLucidAuth:Mode=DevelopmentBypass</c> and
    /// <c>Authentication:ApiKey:DevelopmentBypassAll=true</c>. Call before registering authentication services.
    /// </remarks>
    public static void GuardAllDevelopmentBypasses(
        IConfiguration configuration,
        IHostEnvironment hostEnvironment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(hostEnvironment);

        if (!IsProductionEnvironment(hostEnvironment, configuration))
        {
            return;
        }

        string? mode = ArchLucidConfigurationBridge.ResolveAuthConfigurationValue(configuration, "Mode");

        if (string.Equals(mode, "DevelopmentBypass", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(DevelopmentBypassNotAllowedMessage);
        }

        if (configuration.GetValue("Authentication:ApiKey:DevelopmentBypassAll", false))
        {
            throw new InvalidOperationException(DevelopmentBypassAllNotAllowedMessage);
        }
    }

    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> when auth mode is DevelopmentBypass and the host is
    /// treated as Production (<see cref="IHostEnvironment.IsProduction"/> or <c>ARCHLUCID_ENVIRONMENT=Production</c>).
    /// </summary>
    /// <remarks>Delegates to <see cref="GuardAllDevelopmentBypasses"/> so API-key open-access flags are enforced in the same pass.</remarks>
    public static void GuardDevelopmentBypassInProduction(
        IConfiguration configuration,
        IHostEnvironment hostEnvironment)
    {
        GuardAllDevelopmentBypasses(configuration, hostEnvironment);
    }

    private static bool IsProductionEnvironment(IHostEnvironment hostEnvironment, IConfiguration configuration)
    {
        if (hostEnvironment.IsProduction())
        {
            return true;
        }

        if (EnvironmentNameImpliesProductionLike(hostEnvironment.EnvironmentName))
        {
            return true;
        }

        string? archLucidEnv = configuration["ARCHLUCID_ENVIRONMENT"];

        if (string.IsNullOrWhiteSpace(archLucidEnv))
        {
            archLucidEnv = Environment.GetEnvironmentVariable("ARCHLUCID_ENVIRONMENT");
        }

        if (EnvironmentNameImpliesProductionLike(archLucidEnv))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Treats names containing <c>prod</c> (case-insensitive) as production-like so misnamed hosts
    /// (for example <c>PreProduction</c>, <c>staging-prod</c>) cannot run DevelopmentBypass.
    /// Excludes <c>non-production</c> / <c>nonproduction</c> so dev stacks that embed that phrase are not blocked.
    /// </summary>
    private static bool EnvironmentNameImpliesProductionLike(string? environmentName)
    {
        if (string.IsNullOrWhiteSpace(environmentName))
        {
            return false;
        }

        string trimmed = environmentName.Trim();

        if (trimmed.Contains("non-production", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (trimmed.Contains("nonproduction", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return trimmed.Contains("prod", StringComparison.OrdinalIgnoreCase);
    }
}
