using ArchLucid.Host.Core.Configuration;

namespace ArchLucid.Host.Core.Startup.Validation.Rules;

internal static class AuthenticationRules
{
    public static void CollectApiKeyWhenEnabled(IConfiguration configuration, List<string> errors)
    {
        bool apiKeyEnabled = configuration.GetValue("Authentication:ApiKey:Enabled", false);

        if (!apiKeyEnabled)
            return;

        string? adminKey = configuration["Authentication:ApiKey:AdminKey"];
        string? readerKey = configuration["Authentication:ApiKey:ReadOnlyKey"];

        if (string.IsNullOrWhiteSpace(adminKey) && string.IsNullOrWhiteSpace(readerKey))
            errors.Add(
                "When Authentication:ApiKey:Enabled is true, at least one of Authentication:ApiKey:AdminKey or Authentication:ApiKey:ReadOnlyKey must be configured.");
    }

    public static void CollectProductionApiKeyBypass(IConfiguration configuration, List<string> errors)
    {
        if (configuration.GetValue("Authentication:ApiKey:DevelopmentBypassAll", false))
            errors.Add("Authentication:ApiKey:DevelopmentBypassAll must be false in Production.");
    }

    /// <summary>
    /// When API keys are enabled in Production, rejects placeholder-like or overly short configured keys.
    /// Applies to API and Worker hosts (runs before Worker-only early return).
    /// </summary>
    public static void CollectProductionApiKeyPlaceholders(IConfiguration configuration, List<string> errors)
    {
        if (!configuration.GetValue("Authentication:ApiKey:Enabled", false))
            return;

        string? adminKey = configuration["Authentication:ApiKey:AdminKey"];

        if (!string.IsNullOrWhiteSpace(adminKey) && ApiKeyPlaceholderDetection.IsPlaceholderValue(adminKey))
            errors.Add(
                "Authentication:ApiKey:AdminKey appears to be a placeholder or weak value. Use a cryptographically random key of at least 20 characters in Production.");

        string? readOnlyKey = configuration["Authentication:ApiKey:ReadOnlyKey"];

        if (!string.IsNullOrWhiteSpace(readOnlyKey) && ApiKeyPlaceholderDetection.IsPlaceholderValue(readOnlyKey))

            errors.Add(
                "Authentication:ApiKey:ReadOnlyKey appears to be a placeholder or weak value. Use a cryptographically random key of at least 20 characters in Production.");
    }

    /// <summary>
    /// When <c>ArchLucidAuth:JwtSigningPublicKeyPemPath</c> is set (local RSA public key), require issuer and audience.
    /// Applies in all environments so misconfigured CI or dev hosts fail fast.
    /// </summary>
    public static void CollectJwtBearerLocalSigningKey(IConfiguration configuration, List<string> errors)
    {
        string? authMode = ArchLucidConfigurationBridge.ResolveAuthConfigurationValue(configuration, "Mode");

        if (!string.Equals(authMode, "JwtBearer", StringComparison.OrdinalIgnoreCase))
            return;

        string? pemPath =
            ArchLucidConfigurationBridge.ResolveAuthConfigurationValue(configuration, "JwtSigningPublicKeyPemPath");

        if (string.IsNullOrWhiteSpace(pemPath))
            return;

        if (string.IsNullOrWhiteSpace(
                ArchLucidConfigurationBridge.ResolveAuthConfigurationValue(configuration, "JwtLocalIssuer")))
            errors.Add(
                "ArchLucidAuth:JwtLocalIssuer is required when ArchLucidAuth:JwtSigningPublicKeyPemPath is set.");

        if (string.IsNullOrWhiteSpace(
                ArchLucidConfigurationBridge.ResolveAuthConfigurationValue(configuration, "JwtLocalAudience")))
            errors.Add(
                "ArchLucidAuth:JwtLocalAudience is required when ArchLucidAuth:JwtSigningPublicKeyPemPath is set.");
    }

    /// <summary>JwtBearer / ApiKey production checks for API hosts (not Worker).</summary>
    public static void CollectProductionAuthModes(IConfiguration configuration, List<string> errors)
    {
        string? authMode = ArchLucidConfigurationBridge.ResolveAuthConfigurationValue(configuration, "Mode");

        if (!IsWellKnownAuthMode(authMode))
            errors.Add(
                "ArchLucidAuth:Mode must be ApiKey, JwtBearer, or DevelopmentBypass. "
                + "Unrecognized values are not allowed (they are treated as an unsupported auth path at startup).");

        if (configuration.GetValue("ArchLucidAuth:RequireJwtBearerInProduction", false) &&
            !string.Equals(authMode, "JwtBearer", StringComparison.OrdinalIgnoreCase))
            errors.Add(
                "ArchLucidAuth:RequireJwtBearerInProduction is true: Production must use ArchLucidAuth:Mode=JwtBearer (Entra or OIDC Authority).");

        if (string.Equals(authMode, "DevelopmentBypass", StringComparison.OrdinalIgnoreCase))
            errors.Add(
                "ArchLucidAuth:Mode cannot be DevelopmentBypass when the host environment is Production.");

        if (string.Equals(authMode, "JwtBearer", StringComparison.OrdinalIgnoreCase))
        {
            string? pemPath =
                ArchLucidConfigurationBridge.ResolveAuthConfigurationValue(configuration, "JwtSigningPublicKeyPemPath");

            if (!string.IsNullOrWhiteSpace(pemPath))

                errors.Add(
                    "ArchLucidAuth:JwtSigningPublicKeyPemPath is set; local JWT validation is for non-production / CI only and must not be used in Production.");

            else if (string.IsNullOrWhiteSpace(
                         ArchLucidConfigurationBridge.ResolveAuthConfigurationValue(configuration, "Authority")))
                errors.Add(
                    "ArchLucidAuth:Authority is required when auth Mode is JwtBearer in Production.");
        }

        if (!string.Equals(authMode, "ApiKey", StringComparison.OrdinalIgnoreCase))
            return;

        if (!configuration.GetValue("Authentication:ApiKey:Enabled", false))
            errors.Add(
                "Authentication:ApiKey:Enabled must be true when ArchLucidAuth:Mode is ApiKey in Production.");

        string? productionApiAdminKey = configuration["Authentication:ApiKey:AdminKey"];
        string? productionApiReaderKey = configuration["Authentication:ApiKey:ReadOnlyKey"];

        if (string.IsNullOrWhiteSpace(productionApiAdminKey) && string.IsNullOrWhiteSpace(productionApiReaderKey))

            errors.Add(
                "Production ApiKey auth requires at least one of Authentication:ApiKey:AdminKey or Authentication:ApiKey:ReadOnlyKey.");
    }


    private static bool IsWellKnownAuthMode(string? mode)
    {
        // Omitted in JSON binds to the ArchLucidAuthOptions class default (ApiKey).
        if (string.IsNullOrWhiteSpace(mode))
            return true;

        if (string.Equals(mode, "JwtBearer", StringComparison.OrdinalIgnoreCase))
            return true;

        return string.Equals(mode, "ApiKey", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(mode, "DevelopmentBypass", StringComparison.OrdinalIgnoreCase);
    }
}
