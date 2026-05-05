using ArchLucid.Core.Configuration;

using Microsoft.Extensions.Configuration;

namespace ArchLucid.Cli.Commands;

internal static class ValidateConfigEvaluator
{
    private const string ArchLucidAuthPrefix = "ArchLucidAuth";

    internal static IReadOnlyList<ValidateConfigFinding> Evaluate(
        IConfiguration configuration,
        string contentRoot,
        bool appsettingsExists)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        ArgumentException.ThrowIfNullOrEmpty(contentRoot);

        List<ValidateConfigFinding> findings = [];

        AppendEnvironmentFacts(findings, configuration, contentRoot, appsettingsExists);

        AppendStorageRules(findings, configuration);

        AppendAgentExecutionRules(findings, configuration);

        AppendEntraJwtRules(findings, configuration);

        AppendApiKeyRules(findings, configuration);

        AppendAzureOpenAiRules(findings, configuration);

        return findings.AsReadOnly();
    }

    private static void AppendEnvironmentFacts(
        List<ValidateConfigFinding> findings,
        IConfiguration configuration,
        string contentRoot,
        bool appsettingsExists)
    {
        string effectiveEnv =
            configuration["ASPNETCORE_ENVIRONMENT"]?.Trim()
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "(unset — treated as Production for appsettings overlay)";

        findings.Add(new ValidateConfigFinding(
            ValidateConfigFindingSeverity.Info,
            "Bootstrap",
            "Content root",
            Path.GetFullPath(contentRoot)));

        findings.Add(new ValidateConfigFinding(
            ValidateConfigFindingSeverity.Info,
            "Bootstrap",
            "Hosting environment key",
            effectiveEnv));

        if (appsettingsExists)

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Ok,
                "Bootstrap",
                "appsettings.json",
                "Present on disk."));

        else

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Warning,
                "Bootstrap",
                "appsettings.json",
                "Not found — only environment variables / other layers apply."));
    }

    private static void AppendStorageRules(List<ValidateConfigFinding> findings, IConfiguration configuration)
    {
        string? storageRaw = configuration["ArchLucid:StorageProvider"]?.Trim();

        if (string.IsNullOrWhiteSpace(storageRaw))

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Ok,
                "Database",
                "ArchLucid:StorageProvider",
                "Unset defaults to Sql (product rule)."));

        else if (!string.Equals(storageRaw, "Sql", StringComparison.OrdinalIgnoreCase)
                 && !string.Equals(storageRaw, "InMemory", StringComparison.OrdinalIgnoreCase))

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Error,
                "Database",
                "ArchLucid:StorageProvider",
                "Must be Sql or InMemory when set."));

        else

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Ok,
                "Database",
                "ArchLucid:StorageProvider",
                $"{storageRaw} — storage mode recognized."));

        bool storageSql = string.IsNullOrWhiteSpace(storageRaw)
                          || string.Equals(storageRaw, "Sql", StringComparison.OrdinalIgnoreCase);

        if (!storageSql)
        {
            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Ok,
                "Database",
                "ConnectionStrings:ArchLucid",
                "Not required when ArchLucid:StorageProvider is InMemory."));

            return;
        }

        string? cs = configuration.GetConnectionString("ArchLucid");

        if (string.IsNullOrWhiteSpace(cs))

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Error,
                "Database",
                "ConnectionStrings:ArchLucid",
                "Required for Sql storage (use ConnectionStrings__ArchLucid env or appsettings)."));

        else
        {
            if (LooksLikeSqlServerConnectionString(cs))

                findings.Add(new ValidateConfigFinding(
                    ValidateConfigFindingSeverity.Ok,
                    "Database",
                    "ConnectionStrings:ArchLucid",
                    "Present (value not shown). Recognized SQL Server-style keys."));

            else

                findings.Add(new ValidateConfigFinding(
                    ValidateConfigFindingSeverity.Warning,
                    "Database",
                    "ConnectionStrings:ArchLucid",
                    "Present but does not look like a typical SQL Server connection string (check Server= or Data Source=)."));
        }
    }

    private static void AppendAgentExecutionRules(List<ValidateConfigFinding> findings, IConfiguration configuration)
    {
        string? agentMode = configuration["AgentExecution:Mode"]?.Trim();

        if (string.IsNullOrWhiteSpace(agentMode))

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Warning,
                "AgentExecution",
                "AgentExecution:Mode",
                "Unset — confirm the host default matches your intent (template uses Simulator)."));

        else if (!string.Equals(agentMode, "Simulator", StringComparison.OrdinalIgnoreCase)
                 && !string.Equals(agentMode, "Real", StringComparison.OrdinalIgnoreCase))

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Error,
                "AgentExecution",
                "AgentExecution:Mode",
                $"Invalid value '{agentMode}' — must be Simulator or Real."));

        else

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Ok,
                "AgentExecution",
                "AgentExecution:Mode",
                agentMode));

        string? completionClient = configuration["AgentExecution:CompletionClient"]?.Trim();

        if (string.IsNullOrEmpty(completionClient))

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Ok,
                "AgentExecution",
                "AgentExecution:CompletionClient",
                "Omitted — Real mode uses Azure OpenAI when not set to Echo."));

        else
        {
            bool echo = string.Equals(completionClient, "Echo", StringComparison.OrdinalIgnoreCase);

            bool azure = string.Equals(completionClient, "AzureOpenAi", StringComparison.OrdinalIgnoreCase);

            if (!echo && !azure)

                findings.Add(new ValidateConfigFinding(
                    ValidateConfigFindingSeverity.Error,
                    "AgentExecution",
                    "AgentExecution:CompletionClient",
                    $"Invalid value '{completionClient}' — must be Echo, AzureOpenAi, or omitted."));

            else

                findings.Add(new ValidateConfigFinding(
                    ValidateConfigFindingSeverity.Ok,
                    "AgentExecution",
                    "AgentExecution:CompletionClient",
                    completionClient));
        }

        int maxCompletionTokens = configuration.GetValue("AzureOpenAI:MaxCompletionTokens", 0);

        if (maxCompletionTokens is < 0 or > 262_144)

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Error,
                "AzureOpenAI",
                "AzureOpenAI:MaxCompletionTokens",
                "Must be 0 (use default) or between 1 and 262144 inclusive."));

        else if (maxCompletionTokens > 0)

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Ok,
                "AzureOpenAI",
                "AzureOpenAI:MaxCompletionTokens",
                $"Set to {maxCompletionTokens}."));

        else

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Ok,
                "AzureOpenAI",
                "AzureOpenAI:MaxCompletionTokens",
                "0 / omitted — host uses built-in default (4096)."));
    }

    private static void AppendEntraJwtRules(List<ValidateConfigFinding> findings, IConfiguration configuration)
    {
        string? authMode = configuration[$"{ArchLucidAuthPrefix}:Mode"]?.Trim();

        if (string.IsNullOrWhiteSpace(authMode))

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Ok,
                "Entra / OIDC",
                "ArchLucidAuth:Mode",
                "Unset — binds to default ApiKey in product (see template)."));

        else if (!IsWellKnownAuthMode(authMode))

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Error,
                "Entra / OIDC",
                "ArchLucidAuth:Mode",
                $"Unrecognized value '{authMode}' — must be ApiKey, JwtBearer, or DevelopmentBypass."));

        else

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Ok,
                "Entra / OIDC",
                "ArchLucidAuth:Mode",
                authMode));

        if (!string.Equals(authMode, "JwtBearer", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Ok,
                "Entra / OIDC",
                "OIDC metadata (Authority / Audience)",
                "Skipped — ArchLucidAuth:Mode is not JwtBearer."));

            return;
        }

        string? pemPath = configuration[$"{ArchLucidAuthPrefix}:JwtSigningPublicKeyPemPath"]?.Trim();

        if (!string.IsNullOrWhiteSpace(pemPath))
        {
            bool issuerOk = ConfigurationKeyPresence.IsValuePresent(configuration, $"{ArchLucidAuthPrefix}:JwtLocalIssuer");

            bool audienceOk = ConfigurationKeyPresence.IsValuePresent(configuration, $"{ArchLucidAuthPrefix}:JwtLocalAudience");

            if (!issuerOk)

                findings.Add(new ValidateConfigFinding(
                    ValidateConfigFindingSeverity.Error,
                    "Entra / OIDC",
                    "ArchLucidAuth:JwtLocalIssuer",
                    "Required when JwtSigningPublicKeyPemPath is set (local RSA public key validation)."));

            else

                findings.Add(new ValidateConfigFinding(
                    ValidateConfigFindingSeverity.Ok,
                    "Entra / OIDC",
                    "ArchLucidAuth:JwtLocalIssuer",
                    "Present."));

            if (!audienceOk)

                findings.Add(new ValidateConfigFinding(
                    ValidateConfigFindingSeverity.Error,
                    "Entra / OIDC",
                    "ArchLucidAuth:JwtLocalAudience",
                    "Required when JwtSigningPublicKeyPemPath is set."));

            else

                findings.Add(new ValidateConfigFinding(
                    ValidateConfigFindingSeverity.Ok,
                    "Entra / OIDC",
                    "ArchLucidAuth:JwtLocalAudience",
                    "Present."));

            return;
        }

        if (!ConfigurationKeyPresence.IsValuePresent(configuration, $"{ArchLucidAuthPrefix}:Authority"))

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Error,
                "Entra / OIDC",
                "ArchLucidAuth:Authority",
                "Required for JwtBearer with Entra / OIDC (HTTPS issuer / metadata URL)."));

        else if (!TryCreateHttpsUri(configuration[$"{ArchLucidAuthPrefix}:Authority"]))

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Error,
                "Entra / OIDC",
                "ArchLucidAuth:Authority",
                "Must be an absolute HTTPS URI (Entra v2.0 issuer or OIDC metadata base)."));

        else

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Ok,
                "Entra / OIDC",
                "ArchLucidAuth:Authority",
                "Present and HTTPS (value not shown)."));

        if (!ConfigurationKeyPresence.IsValuePresent(configuration, $"{ArchLucidAuthPrefix}:Audience"))

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Error,
                "Entra / OIDC",
                "ArchLucidAuth:Audience",
                "Required for JwtBearer (API app id URI / audience)."));

        else

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Ok,
                "Entra / OIDC",
                "ArchLucidAuth:Audience",
                "Present (value not shown)."));
    }

    private static void AppendApiKeyRules(List<ValidateConfigFinding> findings, IConfiguration configuration)
    {
        if (!configuration.GetValue("Authentication:ApiKey:Enabled", false))
        {
            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Ok,
                "API key auth",
                "Authentication:ApiKey",
                "Disabled — AdminKey/ReadOnlyKey not required."));

            return;
        }

        string? adminKey = configuration["Authentication:ApiKey:AdminKey"];

        string? readKey = configuration["Authentication:ApiKey:ReadOnlyKey"];

        if (string.IsNullOrWhiteSpace(adminKey) && string.IsNullOrWhiteSpace(readKey))

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Error,
                "API key auth",
                "Authentication:ApiKey key material",
                "At least one of AdminKey or ReadOnlyKey must be set when Enabled is true."));

        else

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Ok,
                "API key auth",
                "Authentication:ApiKey key material",
                "At least one key is present (values not shown)."));
    }

    private static void AppendAzureOpenAiRules(List<ValidateConfigFinding> findings, IConfiguration configuration)
    {
        if (!string.Equals(
                configuration["AgentExecution:Mode"]?.Trim(),
                "Real",
                StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Ok,
                "Azure OpenAI",
                "AzureOpenAI:*",
                "Skipped — AgentExecution:Mode is not Real."));

            return;
        }

        string? completionClient = configuration["AgentExecution:CompletionClient"]?.Trim();

        if (string.Equals(completionClient, "Echo", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Ok,
                "Azure OpenAI",
                "AzureOpenAI:*",
                "Skipped — Echo completion client does not call Azure OpenAI."));

            return;
        }

        string? endpoint = configuration["AzureOpenAI:Endpoint"]?.Trim();

        string? apiKey = configuration["AzureOpenAI:ApiKey"]?.Trim();

        string? deployment = configuration["AzureOpenAI:DeploymentName"]?.Trim();

        if (string.IsNullOrWhiteSpace(endpoint))

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Error,
                "Azure OpenAI",
                "AzureOpenAI:Endpoint",
                "Required for Real mode (set AzureOpenAI:Endpoint or AZURE_OPENAI__Endpoint)."));

        else if (!TryCreateHttpsUri(endpoint))

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Error,
                "Azure OpenAI",
                "AzureOpenAI:Endpoint",
                "Must be an absolute HTTPS URI."));

        else

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Ok,
                "Azure OpenAI",
                "AzureOpenAI:Endpoint",
                "Present and HTTPS (value not shown)."));

        if (string.IsNullOrWhiteSpace(apiKey))

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Error,
                "Azure OpenAI",
                "AzureOpenAI:ApiKey",
                "Required for Real mode (secret — use Key Vault or AZURE_OPENAI__ApiKey)."));

        else

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Ok,
                "Azure OpenAI",
                "AzureOpenAI:ApiKey",
                "Present (value not shown)."));

        if (string.IsNullOrWhiteSpace(deployment))

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Error,
                "Azure OpenAI",
                "AzureOpenAI:DeploymentName",
                "Required — deployment name for chat/completions."));

        else

            findings.Add(new ValidateConfigFinding(
                ValidateConfigFindingSeverity.Ok,
                "Azure OpenAI",
                "AzureOpenAI:DeploymentName",
                "Present (value not shown)."));
    }

    private static bool LooksLikeSqlServerConnectionString(string connectionString)
    {
        return connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase)
               || connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryCreateHttpsUri(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out Uri? parsed))
            return false;

        return parsed.Scheme == Uri.UriSchemeHttps;
    }

    private static bool IsWellKnownAuthMode(string mode) =>
        string.Equals(mode, "ApiKey", StringComparison.OrdinalIgnoreCase)
        || string.Equals(mode, "JwtBearer", StringComparison.OrdinalIgnoreCase)
        || string.Equals(mode, "DevelopmentBypass", StringComparison.OrdinalIgnoreCase);
}
