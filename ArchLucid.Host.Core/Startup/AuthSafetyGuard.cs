using ArchLucid.Core.Hosting;
using ArchLucid.Host.Core.Configuration;

namespace ArchLucid.Host.Core.Startup;

/// <summary>Fail-fast checks for dangerous authentication configuration combinations.</summary>
public static class AuthSafetyGuard
{
    /// <summary>
    ///     Thrown when <c>ArchLucidAuth:Mode</c> is <c>DevelopmentBypass</c> but the host environment name is not
    ///     <see cref="Environments.Development" /> (see <see cref="IHostEnvironment.IsDevelopment()" />).
    /// </summary>
    public const string DevelopmentBypassOutsideDevelopmentMessage =
        "DevelopmentBypass auth mode is not allowed outside Development environments. "
        + "Set ArchLucidAuth:Mode to ApiKey or JwtBearer.";

    /// <summary>
    ///     Thrown when <c>ArchLucidAuth:Mode</c> is not <c>JwtBearer</c> or <c>ApiKey</c> outside Development —
    ///     the API host would otherwise register <c>DevelopmentBypassAuthenticationHandler</c> as the default scheme.
    /// </summary>
    public const string AuthModeJwtOrApiKeyRequiredOutsideDevelopmentMessage =
        "ArchLucidAuth:Mode must be JwtBearer or ApiKey outside Development environments. "
        + "Any other value (including omitting Mode) registers DevelopmentBypass authentication, which is not permitted.";

    private const string DevelopmentBypassAllNotAllowedOutsideDevelopmentMessage =
        "Authentication:ApiKey:DevelopmentBypassAll is not permitted outside Development environments. "
        + "Set Authentication:ApiKey:DevelopmentBypassAll to false.";

    /// <summary>
    ///     Throws <see cref="InvalidOperationException" /> when any production-incompatible development auth
    ///     setting is active and the host is treated as production-like: <see cref="IHostEnvironment.IsProduction" />,
    ///     <c>ASPNETCORE_ENVIRONMENT</c> / <c>ARCHLUCID_ENVIRONMENT</c> names containing <c>prod</c> (excluding
    ///     <c>non-production</c> / <c>nonproduction</c>), or exact Production via configuration.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <c>ArchLucidAuth:Mode=DevelopmentBypass</c> is only allowed when
    ///         <see cref="IHostEnvironment.IsDevelopment()" /> is true (so Staging/Production/Test hosts cannot
    ///         accidentally ship with bypass auth even if <c>ASPNETCORE_ENVIRONMENT</c> is mis-set).
    ///     </para>
    ///     <para>
    ///         When DevelopmentBypass is active in Development, <paramref name="logger" /> receives a warning
    ///         (include <c>ArchLucidAuth:DevUserId</c>); pass <see langword="null" /> in unit tests if not needed.
    ///     </para>
    ///     <para>
    ///         <c>Authentication:ApiKey:DevelopmentBypassAll=true</c> remains blocked in any
    ///         environment where <see cref="IHostEnvironment.IsDevelopment()" /> is <see langword="false" />
    ///         (Staging, Production, Test, and production-like names). Call before registering authentication services.
    ///     </para>
    /// </remarks>
    public static void GuardAllDevelopmentBypasses(
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(hostEnvironment);

        string? mode = ArchLucidConfigurationBridge.ResolveAuthConfigurationValue(configuration, "Mode");

        if (string.Equals(mode, "DevelopmentBypass", StringComparison.OrdinalIgnoreCase))
        {
            bool productionLike = IsProductionEnvironment(hostEnvironment, configuration);

            if (!hostEnvironment.IsDevelopment() || productionLike)
                throw new InvalidOperationException(DevelopmentBypassOutsideDevelopmentMessage);

            string devUserId =
                ArchLucidConfigurationBridge.ResolveAuthConfigurationValue(configuration, "DevUserId") ?? "dev-user";

            logger?.LogWarning(
                "DevelopmentBypass auth mode is active. All requests are authenticated as {DevUserId}. Do not use in production.",
                devUserId);
        }
        else if (!hostEnvironment.IsDevelopment())
        {
            bool isJwt = string.Equals(mode, "JwtBearer", StringComparison.OrdinalIgnoreCase);
            bool isApiKey = string.Equals(mode, "ApiKey", StringComparison.OrdinalIgnoreCase);

            if (!isJwt && !isApiKey)
                throw new InvalidOperationException(AuthModeJwtOrApiKeyRequiredOutsideDevelopmentMessage);
        }


        if (configuration.GetValue("Authentication:ApiKey:DevelopmentBypassAll", false) && !hostEnvironment.IsDevelopment())
            throw new InvalidOperationException(DevelopmentBypassAllNotAllowedOutsideDevelopmentMessage);
    }

    /// <summary>
    ///     Same guard as <see cref="GuardAllDevelopmentBypasses" /> (DevelopmentBypass rules + production-like
    ///     <c>DevelopmentBypassAll</c> check). Prefer calling <see cref="GuardAllDevelopmentBypasses" /> from new code.
    /// </summary>
    /// <remarks>Delegates to <see cref="GuardAllDevelopmentBypasses" /> so API-key open-access flags are enforced in the same pass.</remarks>
    public static void GuardDevelopmentBypassInProduction(
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ILogger? logger = null)
    {
        GuardAllDevelopmentBypasses(configuration, hostEnvironment, logger);
    }

    private static bool IsProductionEnvironment(IHostEnvironment hostEnvironment, IConfiguration configuration)
    {
        if (hostEnvironment.IsProduction())
            return true;


        if (HostingEnvironmentNamePatterns.EnvironmentNameImpliesProductionLike(hostEnvironment.EnvironmentName))
            return true;


        string? archLucidEnv = configuration["ARCHLUCID_ENVIRONMENT"];

        if (string.IsNullOrWhiteSpace(archLucidEnv))

            archLucidEnv = Environment.GetEnvironmentVariable("ARCHLUCID_ENVIRONMENT");


        return HostingEnvironmentNamePatterns.EnvironmentNameImpliesProductionLike(archLucidEnv);
    }
}
