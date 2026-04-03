using System.Globalization;

using ArchiForge.Api.Configuration;

using ArchiForge.DecisionEngine.Validation;
using ArchiForge.Persistence.Caching;
using ArchiForge.Persistence.Archival;
using ArchiForge.Retrieval.Indexing;

namespace ArchiForge.Api.Startup.Validation;

/// <summary>
/// Central rules for ArchiForge API configuration. Used for fail-fast startup validation before migrations.
/// </summary>
public static class ArchiForgeConfigurationRules
{
    /// <summary>
    /// Collects human-readable configuration errors. Empty list means configuration is acceptable to start the host.
    /// </summary>
    public static IReadOnlyList<string> CollectErrors(
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        List<string> errors = [];

        ArchiForgeOptions archiForge =
            configuration.GetSection(ArchiForgeOptions.SectionName).Get<ArchiForgeOptions>() ?? new ArchiForgeOptions();

        bool storageIsSql = string.Equals(archiForge.StorageProvider, "Sql", StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(archiForge.StorageProvider) &&
            !string.Equals(archiForge.StorageProvider, "InMemory", StringComparison.OrdinalIgnoreCase) &&
            !storageIsSql)
        
            errors.Add("ArchiForge:StorageProvider must be 'InMemory' or 'Sql' when set.");
        

        string? connectionString = configuration.GetConnectionString("ArchiForge");
        if (storageIsSql && string.IsNullOrWhiteSpace(connectionString))
        
            errors.Add(
                "ConnectionStrings:ArchiForge is required when ArchiForge:StorageProvider is Sql (or unset, defaulting to Sql).");
        

        bool apiKeyEnabled = configuration.GetValue("Authentication:ApiKey:Enabled", false);
        if (apiKeyEnabled)
        {
            string? adminKey = configuration["Authentication:ApiKey:AdminKey"];
            string? readerKey = configuration["Authentication:ApiKey:ReadOnlyKey"];
            if (string.IsNullOrWhiteSpace(adminKey) && string.IsNullOrWhiteSpace(readerKey))
            
                errors.Add(
                    "When Authentication:ApiKey:Enabled is true, at least one of Authentication:ApiKey:AdminKey or Authentication:ApiKey:ReadOnlyKey must be configured.");
            
        }

        string? agentMode = configuration["AgentExecution:Mode"];
        if (!string.IsNullOrWhiteSpace(agentMode) &&
            !string.Equals(agentMode, "Simulator", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(agentMode, "Real", StringComparison.OrdinalIgnoreCase))
        
            errors.Add("AgentExecution:Mode must be either 'Simulator' or 'Real'.");
        

        if (string.Equals(agentMode, "Real", StringComparison.OrdinalIgnoreCase))
        {
            string? endpoint = configuration["AzureOpenAI:Endpoint"];
            string? apiKey = configuration["AzureOpenAI:ApiKey"];
            string? deployment = configuration["AzureOpenAI:DeploymentName"];
            if (string.IsNullOrWhiteSpace(endpoint) ||
                string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(deployment))
            
                errors.Add(
                    "AgentExecution:Mode is 'Real' but one or more AzureOpenAI settings (Endpoint, ApiKey, DeploymentName) are missing.");
            
        }

        CollectSchemaFileErrors(configuration, errors);
        CollectBatchReplayErrors(configuration, errors);
        CollectApiDeprecationErrors(configuration, errors);
        CollectDataArchivalErrors(configuration, errors);
        CollectRetrievalEmbeddingCapErrors(configuration, errors);
        CollectRetrievalVectorIndexErrors(configuration, errors);
        CollectRateLimitingErrors(configuration, errors);
        CollectHotPathCacheErrors(configuration, errors);

        if (!environment.IsProduction())
        
            return errors;
        

        CollectProductionSafetyErrors(configuration, errors);

        string? authMode = configuration["ArchiForgeAuth:Mode"];
        if (string.Equals(authMode, "DevelopmentBypass", StringComparison.OrdinalIgnoreCase))

            errors.Add("ArchiForgeAuth:Mode cannot be DevelopmentBypass when the host environment is Production.");


        if (string.Equals(authMode, "JwtBearer", StringComparison.OrdinalIgnoreCase))

            if (string.IsNullOrWhiteSpace(configuration["ArchiForgeAuth:Authority"]))

                errors.Add(
                    "ArchiForgeAuth:Authority is required when ArchiForgeAuth:Mode is JwtBearer in Production.");


        if (!string.Equals(authMode, "ApiKey", StringComparison.OrdinalIgnoreCase)) return errors;

        if (!configuration.GetValue("Authentication:ApiKey:Enabled", false))

            errors.Add(
                "Authentication:ApiKey:Enabled must be true when ArchiForgeAuth:Mode is ApiKey in Production.");


        string? productionApiAdminKey = configuration["Authentication:ApiKey:AdminKey"];
        string? productionApiReaderKey = configuration["Authentication:ApiKey:ReadOnlyKey"];
        if (string.IsNullOrWhiteSpace(productionApiAdminKey) && string.IsNullOrWhiteSpace(productionApiReaderKey))

            errors.Add(
                "Production ApiKey auth requires at least one of Authentication:ApiKey:AdminKey or Authentication:ApiKey:ReadOnlyKey.");


        return errors;
    }

    /// <summary>
    /// Fail-fast checks for CORS and outbound webhook HMAC in Production only.
    /// </summary>
    private static void CollectProductionSafetyErrors(IConfiguration configuration, List<string> errors)
    {
        string[]? origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (origins is null || origins.Length == 0)
        
            errors.Add("Production requires at least one Cors:AllowedOrigins entry.");
        
        else
        
            foreach (string? origin in origins)
            {
                if (string.IsNullOrWhiteSpace(origin))
                
                    continue;
                

                string trimmed = origin.Trim();

                if (string.Equals(trimmed, "*", StringComparison.Ordinal))
                
                    errors.Add("Cors:AllowedOrigins must not use a wildcard '*' in Production.");
                
            }
        

        WebhookDeliveryOptions webhook =
            configuration.GetSection(WebhookDeliveryOptions.SectionName).Get<WebhookDeliveryOptions>() ??
            new WebhookDeliveryOptions();

        const int minWebhookSecretChars = 32;

        if (!webhook.UseHttpClient)
        
            return;
        

        if (string.IsNullOrWhiteSpace(webhook.HmacSha256SharedSecret))
        {
            errors.Add(
                "WebhookDelivery:HmacSha256SharedSecret is required in Production when WebhookDelivery:UseHttpClient is true.");

            return;
        }

        if (webhook.HmacSha256SharedSecret.Length < minWebhookSecretChars)
        
            errors.Add(
                $"WebhookDelivery:HmacSha256SharedSecret must be at least {minWebhookSecretChars} characters in Production when WebhookDelivery:UseHttpClient is true.");
        
    }

    private static void CollectBatchReplayErrors(IConfiguration configuration, List<string> errors)
    {
        BatchReplayOptions batch =
            configuration.GetSection(BatchReplayOptions.SectionName).Get<BatchReplayOptions>() ?? new BatchReplayOptions();

        const int min = 1;
        const int max = 500;

        if (batch.MaxComparisonRecordIds < min || batch.MaxComparisonRecordIds > max)
        
            errors.Add(
                $"ComparisonReplay:Batch:MaxComparisonRecordIds must be between {min} and {max} (inclusive).");
        
    }

    private static void CollectApiDeprecationErrors(IConfiguration configuration, List<string> errors)
    {
        ApiDeprecationOptions deprecation =
            configuration.GetSection(ApiDeprecationOptions.SectionName).Get<ApiDeprecationOptions>()
            ?? new ApiDeprecationOptions();

        if (!deprecation.Enabled)
        
            return;
        

        string? sunset = deprecation.SunsetHttpDate?.Trim();

        if (string.IsNullOrEmpty(sunset))
        
            return;
        

        if (!DateTimeOffset.TryParse(
                sunset,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out _))
        
            errors.Add(
                "ApiDeprecation:SunsetHttpDate must be empty or a parseable date when ApiDeprecation:Enabled is true.");
        
    }

    private static void CollectDataArchivalErrors(IConfiguration configuration, List<string> errors)
    {
        DataArchivalOptions opts =
            configuration.GetSection(DataArchivalOptions.SectionName).Get<DataArchivalOptions>() ??
            new DataArchivalOptions();

        const int maxDays = 3650;

        if (opts.RunsRetentionDays < 0 || opts.RunsRetentionDays > maxDays)
        
            errors.Add($"DataArchival:RunsRetentionDays must be between 0 and {maxDays} (0 disables run archival).");
        

        if (opts.DigestsRetentionDays < 0 || opts.DigestsRetentionDays > maxDays)
        
            errors.Add($"DataArchival:DigestsRetentionDays must be between 0 and {maxDays} (0 disables digest archival).");
        

        if (opts.ConversationsRetentionDays < 0 || opts.ConversationsRetentionDays > maxDays)
        
            errors.Add(
                $"DataArchival:ConversationsRetentionDays must be between 0 and {maxDays} (0 disables thread archival).");
        

        if (opts.IntervalHours < 1 || opts.IntervalHours > 168)
        
            errors.Add("DataArchival:IntervalHours must be between 1 and 168.");
        
    }

    private static void CollectRateLimitingErrors(IConfiguration configuration, List<string> errors)
    {
        void AddIfInvalid(string path, int permitLimit, int windowMinutes, int queueLimit)
        {
            if (permitLimit < 1)
            
                errors.Add($"{path}:PermitLimit must be at least 1.");
            

            if (windowMinutes < 1)
            
                errors.Add($"{path}:WindowMinutes must be at least 1.");
            

            if (queueLimit < 0)
            
                errors.Add($"{path}:QueueLimit must be 0 or greater.");
            
        }

        IConfigurationSection fixedSection = configuration.GetSection("RateLimiting:FixedWindow");
        if (fixedSection.Exists())
        {
            int permit = configuration.GetValue("RateLimiting:FixedWindow:PermitLimit", 100);
            int window = configuration.GetValue("RateLimiting:FixedWindow:WindowMinutes", 1);
            int queue = configuration.GetValue("RateLimiting:FixedWindow:QueueLimit", 0);
            AddIfInvalid("RateLimiting:FixedWindow", permit, window, queue);
        }

        IConfigurationSection expensiveSection = configuration.GetSection("RateLimiting:Expensive");
        if (expensiveSection.Exists())
        {
            int permit = configuration.GetValue("RateLimiting:Expensive:PermitLimit", 20);
            int window = configuration.GetValue("RateLimiting:Expensive:WindowMinutes", 1);
            int queue = configuration.GetValue("RateLimiting:Expensive:QueueLimit", 0);
            AddIfInvalid("RateLimiting:Expensive", permit, window, queue);
        }

        IConfigurationSection replayLight = configuration.GetSection("RateLimiting:Replay:Light");
        if (replayLight.Exists())
        {
            int permit = configuration.GetValue("RateLimiting:Replay:Light:PermitLimit", 60);
            int window = configuration.GetValue("RateLimiting:Replay:Light:WindowMinutes", 1);
            int queue = configuration.GetValue("RateLimiting:Replay:Light:QueueLimit", 0);
            AddIfInvalid("RateLimiting:Replay:Light", permit, window, queue);
        }

        IConfigurationSection replayHeavy = configuration.GetSection("RateLimiting:Replay:Heavy");
        if (replayHeavy.Exists())
        {
            int permit = configuration.GetValue("RateLimiting:Replay:Heavy:PermitLimit", 15);
            int window = configuration.GetValue("RateLimiting:Replay:Heavy:WindowMinutes", 1);
            int queue = configuration.GetValue("RateLimiting:Replay:Heavy:QueueLimit", 0);
            AddIfInvalid("RateLimiting:Replay:Heavy", permit, window, queue);
        }
    }

    private static void CollectHotPathCacheErrors(IConfiguration configuration, List<string> errors)
    {
        HotPathCacheOptions opts =
            configuration.GetSection(HotPathCacheOptions.SectionName).Get<HotPathCacheOptions>() ??
            new HotPathCacheOptions();

        if (!opts.Enabled)
            return;

        string provider = opts.Provider ?? "Memory";

        if (!string.Equals(provider, "Memory", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(provider, "Redis", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("HotPathCache:Provider must be 'Memory' or 'Redis' when HotPathCache:Enabled is true.");
        }

        if (string.Equals(provider, "Redis", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(opts.RedisConnectionString))
        {
            errors.Add("HotPathCache:RedisConnectionString is required when HotPathCache:Provider is Redis.");
        }

        if (opts.AbsoluteExpirationSeconds > 3600)
        {
            errors.Add("HotPathCache:AbsoluteExpirationSeconds cannot exceed 3600.");
        }
    }

    private static void CollectRetrievalEmbeddingCapErrors(IConfiguration configuration, List<string> errors)
    {
        RetrievalEmbeddingCapOptions caps =
            configuration.GetSection(RetrievalEmbeddingCapOptions.SectionName).Get<RetrievalEmbeddingCapOptions>() ??
            new RetrievalEmbeddingCapOptions();

        if (caps.MaxTextsPerEmbeddingRequest < 1 || caps.MaxTextsPerEmbeddingRequest > 2048)
        
            errors.Add("Retrieval:EmbeddingCaps:MaxTextsPerEmbeddingRequest must be between 1 and 2048.");
        

        if (caps.MaxChunksPerIndexOperation < 0 || caps.MaxChunksPerIndexOperation > 1_000_000)
        
            errors.Add("Retrieval:EmbeddingCaps:MaxChunksPerIndexOperation must be between 0 and 1000000 (0 = unlimited).");
        
    }

    /// <summary>
    /// Aligns with <see cref="ArchiForge.Api.Startup.ServiceCollectionExtensions.RegisterRetrieval"/>: only <c>InMemory</c> and <c>AzureSearch</c> are supported; omitted defaults to in-memory.
    /// </summary>
    private static void CollectRetrievalVectorIndexErrors(IConfiguration configuration, List<string> errors)
    {
        string? mode = configuration["Retrieval:VectorIndex"];

        if (string.IsNullOrWhiteSpace(mode))
        
            return;
        

        if (string.Equals(mode, "InMemory", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mode, "AzureSearch", StringComparison.OrdinalIgnoreCase))
        
            return;
        

        errors.Add(
            "Retrieval:VectorIndex must be 'InMemory', 'AzureSearch', or omitted (defaults to InMemory).");
    }

    private static void CollectSchemaFileErrors(IConfiguration configuration, List<string> errors)
    {
        SchemaValidationOptions opts =
            configuration.GetSection(SchemaValidationOptions.SectionName).Get<SchemaValidationOptions>()
            ?? new SchemaValidationOptions();

        string baseDir = AppContext.BaseDirectory;
        ValidateSchemaPath(opts.AgentResultSchemaPath, "AgentResult", baseDir, errors);
        ValidateSchemaPath(opts.GoldenManifestSchemaPath, "GoldenManifest", baseDir, errors);
    }

    private static void ValidateSchemaPath(
        string relativePath,
        string logicalName,
        string baseDir,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            errors.Add($"SchemaValidation schema path for {logicalName} is missing or empty.");

            return;
        }

        string trimmed = relativePath.Trim();
        if (Path.IsPathRooted(trimmed))
        {
            errors.Add(
                $"SchemaValidation path for {logicalName} must be relative to the application base directory, not an absolute path (got '{trimmed}').");

            return;
        }

        string normalizedBase = Path.GetFullPath(baseDir);
        string fullPath = Path.GetFullPath(Path.Combine(baseDir, trimmed));
        string relativeToBase = Path.GetRelativePath(normalizedBase, fullPath);
        if (relativeToBase.Equals("..", StringComparison.Ordinal) ||
            relativeToBase.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
            relativeToBase.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
        {
            errors.Add(
                $"SchemaValidation path for {logicalName} escapes the application base directory (got '{trimmed}').");

            return;
        }

        if (!File.Exists(fullPath))
        
            errors.Add(
                $"Schema file for {logicalName} was not found at '{fullPath}' (SchemaValidation:*SchemaPath). Ensure content is copied to output (e.g. schemas in project output).");
        
    }
}
