using ArchLucid.Host.Core.Configuration;

namespace ArchLucid.Host.Core.Startup.Validation.Rules;

internal static class AuthenticationRules
{
    public static void CollectApiKeyWhenEnabled(IConfiguration configuration, List<string> errors)
    {
        bool apiKeyEnabled = configuration.GetValue("Authentication:ApiKey:Enabled", false);

        if (!apiKeyEnabled)
        {
            return;
        }

        string? adminKey = configuration["Authentication:ApiKey:AdminKey"];
        string? readerKey = configuration["Authentication:ApiKey:ReadOnlyKey"];

        if (string.IsNullOrWhiteSpace(adminKey) && string.IsNullOrWhiteSpace(readerKey))
        {
            errors.Add(
                "When Authentication:ApiKey:Enabled is true, at least one of Authentication:ApiKey:AdminKey or Authentication:ApiKey:ReadOnlyKey must be configured.");
        }
    }

    public static void CollectProductionApiKeyBypass(IConfiguration configuration, List<string> errors)
    {
        if (configuration.GetValue("Authentication:ApiKey:DevelopmentBypassAll", false))
        {
            errors.Add("Authentication:ApiKey:DevelopmentBypassAll must be false in Production.");
        }
    }

    /// <summary>JwtBearer / ApiKey production checks for API hosts (not Worker).</summary>
    public static void CollectProductionAuthModes(IConfiguration configuration, List<string> errors)
    {
        string? authMode = ArchLucidConfigurationBridge.ResolveAuthConfigurationValue(configuration, "Mode");

        if (string.Equals(authMode, "DevelopmentBypass", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(
                "ArchLucidAuth:Mode (or legacy ArchiForgeAuth:Mode) cannot be DevelopmentBypass when the host environment is Production.");
        }

        if (string.Equals(authMode, "JwtBearer", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(ArchLucidConfigurationBridge.ResolveAuthConfigurationValue(configuration, "Authority")))
            {
                errors.Add(
                    "ArchLucidAuth:Authority (or legacy ArchiForgeAuth:Authority) is required when auth Mode is JwtBearer in Production.");
            }
        }

        if (!string.Equals(authMode, "ApiKey", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!configuration.GetValue("Authentication:ApiKey:Enabled", false))
        {
            errors.Add(
                "Authentication:ApiKey:Enabled must be true when ArchLucidAuth:Mode (or ArchiForgeAuth:Mode) is ApiKey in Production.");
        }

        string? productionApiAdminKey = configuration["Authentication:ApiKey:AdminKey"];
        string? productionApiReaderKey = configuration["Authentication:ApiKey:ReadOnlyKey"];

        if (string.IsNullOrWhiteSpace(productionApiAdminKey) && string.IsNullOrWhiteSpace(productionApiReaderKey))
        {
            errors.Add(
                "Production ApiKey auth requires at least one of Authentication:ApiKey:AdminKey or Authentication:ApiKey:ReadOnlyKey.");
        }
    }
}
