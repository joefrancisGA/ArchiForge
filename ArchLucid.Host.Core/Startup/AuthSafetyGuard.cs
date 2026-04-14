using ArchLucid.Host.Core.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ArchLucid.Host.Core.Startup;

/// <summary>Fail-fast checks for dangerous authentication configuration combinations.</summary>
public static class AuthSafetyGuard
{
    private const string DevelopmentBypassNotAllowedMessage =
        "DevelopmentBypass auth mode is not permitted in Production environments. "
        + "Set ArchLucidAuth:Mode to JwtBearer or ApiKey.";

    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> when auth mode is DevelopmentBypass and the host is
    /// treated as Production (<see cref="IHostEnvironment.IsProduction"/> or <c>ARCHLUCID_ENVIRONMENT=Production</c>).
    /// </summary>
    public static void GuardDevelopmentBypassInProduction(
        IConfiguration configuration,
        IHostEnvironment hostEnvironment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(hostEnvironment);

        string? mode = ArchLucidConfigurationBridge.ResolveAuthConfigurationValue(configuration, "Mode");

        if (!string.Equals(mode, "DevelopmentBypass", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (IsProductionEnvironment(hostEnvironment, configuration))
        {
            throw new InvalidOperationException(DevelopmentBypassNotAllowedMessage);
        }
    }

    private static bool IsProductionEnvironment(IHostEnvironment hostEnvironment, IConfiguration configuration)
    {
        if (hostEnvironment.IsProduction())
        {
            return true;
        }

        string? archLucidEnv = configuration["ARCHLUCID_ENVIRONMENT"];

        if (string.IsNullOrWhiteSpace(archLucidEnv))
        {
            archLucidEnv = Environment.GetEnvironmentVariable("ARCHLUCID_ENVIRONMENT");
        }

        return string.Equals(archLucidEnv?.Trim(), "Production", StringComparison.OrdinalIgnoreCase);
    }
}
