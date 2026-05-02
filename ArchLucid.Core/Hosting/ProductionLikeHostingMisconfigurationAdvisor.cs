using System.Globalization;

using ArchLucid.Core.Diagnostics;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ArchLucid.Core.Hosting;

/// <summary>
///     Non-blocking hints when configuration resembles staging/production hosting but looks unsafe for browser clients
///     or authentication. Logging only — does not change security defaults.
/// </summary>
public static class ProductionLikeHostingMisconfigurationAdvisor
{
    /// <summary>
    ///     Returns human-readable warnings (stable text for CLI and tests). Omits secrets.
    /// </summary>
    public static IReadOnlyList<string> DescribeWarnings(IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        return DescribeWarnings(configuration, environment.EnvironmentName);
    }

    /// <summary>
    ///     CLI and tests: supply <c>ASPNETCORE_ENVIRONMENT</c>-style name (e.g. from environment variables).
    /// </summary>
    public static IReadOnlyList<string> DescribeWarnings(IConfiguration configuration, string hostingEnvironmentName)
    {
        IReadOnlyList<HostingMisconfigurationWarning> structured =
            DescribeWarningRecords(configuration, hostingEnvironmentName);

        return structured.Count is 0
            ? []
            : structured.Select(static w => w.Message).ToList();
    }

    /// <summary>TB-002: exposes stable <paramref name="RuleName"/> for metrics alongside operator <see cref="HostingMisconfigurationWarning.Message"/>.</summary>
    public static IReadOnlyList<HostingMisconfigurationWarning> DescribeWarningRecords(
        IConfiguration configuration,
        string hostingEnvironmentName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (string.IsNullOrWhiteSpace(hostingEnvironmentName))
            throw new ArgumentException("Hosting environment name is required.", nameof(hostingEnvironmentName));

        if (!ShouldEvaluateProductionLikeWarnings(hostingEnvironmentName.Trim(), configuration))
            return [];

        List<HostingMisconfigurationWarning> warnings = [];

        AppendCorsWarnings(configuration, warnings);
        AppendAuthWarnings(configuration, warnings);

        return warnings;
    }

    /// <summary>
    ///     Emits one warning log line per advisory entry when the logger enables <see cref="LogLevel.Warning" />.
    /// </summary>
    public static void LogWarningsIfPresent(ILogger logger, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        foreach (HostingMisconfigurationWarning warning in DescribeWarningRecords(configuration,
                     environment.EnvironmentName))
        {
            if (logger.IsEnabled(LogLevel.Warning))
                logger.LogWarning("[HostingMisconfiguration] {Warning}", warning.Message);

            ArchLucidInstrumentation.RecordStartupConfigWarning(warning.RuleName);
        }
    }

    private static bool ShouldEvaluateProductionLikeWarnings(string environmentName, IConfiguration configuration)
    {
        string? archLucidEnv = ReadArchLucidEnvironment(configuration);

        if (IsDevelopmentEnvironmentName(environmentName))
        {
            bool namingRisk =
                HostingEnvironmentNamePatterns.EnvironmentNameImpliesProductionLike(environmentName)
                || HostingEnvironmentNamePatterns.EnvironmentNameImpliesProductionLike(archLucidEnv);

            bool archLucidStagingOrProd = IsArchLucidEnvironmentStagingOrProduction(archLucidEnv);

            if (!namingRisk && !archLucidStagingOrProd)
                return false;
        }

        if (IsProductionEnvironmentName(environmentName) || IsStagingEnvironmentName(environmentName))
            return true;

        return HostingEnvironmentNamePatterns.EnvironmentNameImpliesProductionLike(environmentName) || IsArchLucidEnvironmentStagingOrProduction(archLucidEnv);
    }

    private static bool IsDevelopmentEnvironmentName(string environmentName) =>
        string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase);

    private static bool IsStagingEnvironmentName(string environmentName) =>
        string.Equals(environmentName, Environments.Staging, StringComparison.OrdinalIgnoreCase);

    private static bool IsProductionEnvironmentName(string environmentName) =>
        string.Equals(environmentName, Environments.Production, StringComparison.OrdinalIgnoreCase);

    private static string? ReadArchLucidEnvironment(IConfiguration configuration)
    {
        string? archLucidEnv = configuration["ARCHLUCID_ENVIRONMENT"];

        if (string.IsNullOrWhiteSpace(archLucidEnv))
            archLucidEnv = Environment.GetEnvironmentVariable("ARCHLUCID_ENVIRONMENT");

        return archLucidEnv;
    }

    private static bool IsArchLucidEnvironmentStagingOrProduction(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string trimmed = value.Trim();

        return string.Equals(trimmed, "Production", StringComparison.OrdinalIgnoreCase)
               || string.Equals(trimmed, "Staging", StringComparison.OrdinalIgnoreCase);
    }

    private static void AppendCorsWarnings(IConfiguration configuration, List<HostingMisconfigurationWarning> warnings)
    {
        if (IsWorkerOnlyHost(configuration))
            return;

        if (HasAnyCorsOrigin(configuration))
            return;

        string hostingRole = configuration["Hosting:Role"]?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(hostingRole))
            hostingRole = "Combined";

        string message =
            string.Format(
                CultureInfo.InvariantCulture,
                "Cors:AllowedOrigins has no entries on a staging/production-like API host (Hosting:Role={0}); "
                + "browsers cannot call the API cross-origin until origins are configured.",
                hostingRole);

        warnings.Add(
            new HostingMisconfigurationWarning(
                ProductionLikeHostingMisconfigurationAdvisorRuleNames.CorsAllowedOriginsEmptyProductionLikeHost,
                message));
    }

    private static bool IsWorkerOnlyHost(IConfiguration configuration)
    {
        string? role = configuration["Hosting:Role"]?.Trim();

        return string.Equals(role, "Worker", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasAnyCorsOrigin(IConfiguration configuration)
    {
        string[]? origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

        if (origins is null || origins.Length == 0)
            return false;

        return origins.Any(static o => !string.IsNullOrWhiteSpace(o));
    }

    private static void AppendAuthWarnings(IConfiguration configuration, List<HostingMisconfigurationWarning> warnings)
    {
        string? mode = ResolveArchLucidAuth(configuration, "Mode");

        if (string.Equals(mode, "JwtBearer", StringComparison.OrdinalIgnoreCase))
        {
            string? pem = ResolveArchLucidAuth(configuration, "JwtSigningPublicKeyPemPath");
            string? authority = ResolveArchLucidAuth(configuration, "Authority");

            if (string.IsNullOrWhiteSpace(pem) && string.IsNullOrWhiteSpace(authority))
                warnings.Add(
                    new HostingMisconfigurationWarning(
                        ProductionLikeHostingMisconfigurationAdvisorRuleNames.JwtBearerMissingAuthorityAndPem,
                        "ArchLucidAuth:Mode is JwtBearer but neither ArchLucidAuth:Authority nor "
                        + "ArchLucidAuth:JwtSigningPublicKeyPemPath is set; JWT authentication cannot succeed."));

            return;
        }

        if (!string.Equals(mode, "ApiKey", StringComparison.OrdinalIgnoreCase))
            return;

        if (!configuration.GetValue("Authentication:ApiKey:Enabled", false))
            warnings.Add(
                new HostingMisconfigurationWarning(
                    ProductionLikeHostingMisconfigurationAdvisorRuleNames.ApiKeyModeDisabledWhenConfigured,
                    "ArchLucidAuth:Mode is ApiKey but Authentication:ApiKey:Enabled is false; "
                    + "configure API keys or switch ArchLucidAuth:Mode."));
    }

    private static string? ResolveArchLucidAuth(IConfiguration configuration, string relativeKey) =>
        configuration[$"ArchLucidAuth:{relativeKey}"]?.Trim();
}
