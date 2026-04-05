using System.Globalization;

using ArchiForge.Core.Integration;
using ArchiForge.DecisionEngine.Validation;
using ArchiForge.Host.Core.Configuration;
using ArchiForge.Host.Core.Hosting;
using ArchiForge.Persistence.Archival;
using ArchiForge.Persistence.BlobStore;
using ArchiForge.Persistence.Caching;
using ArchiForge.Persistence.Connections;
using ArchiForge.Retrieval.Indexing;

namespace ArchiForge.Host.Core.Startup.Validation;

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

        ArchiForgeOptions archiForge = ArchiForgeConfigurationBridge.ResolveArchiForgeOptions(configuration);

        bool storageIsSql = string.Equals(archiForge.StorageProvider, "Sql", StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(archiForge.StorageProvider) &&
            !string.Equals(archiForge.StorageProvider, "InMemory", StringComparison.OrdinalIgnoreCase) &&
            !storageIsSql)

            errors.Add("ArchiForge:StorageProvider must be 'InMemory' or 'Sql' when set.");


        IntegrationEventsOptions integrationEvents =
            configuration.GetSection(IntegrationEventsOptions.SectionName).Get<IntegrationEventsOptions>()
            ?? new IntegrationEventsOptions();

        if (integrationEvents.TransactionalOutboxEnabled && !storageIsSql)
        {
            errors.Add(
                "IntegrationEvents:TransactionalOutboxEnabled requires ArchiForge:StorageProvider Sql (transactional enqueue needs a shared SQL transaction).");
        }

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
            string? completionClient = configuration["AgentExecution:CompletionClient"]?.Trim();
            bool useEchoClient = string.Equals(completionClient, "Echo", StringComparison.OrdinalIgnoreCase);

            if (!string.IsNullOrEmpty(completionClient) &&
                !useEchoClient &&
                !string.Equals(completionClient, "AzureOpenAi", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(
                    "AgentExecution:CompletionClient must be 'Echo', 'AzureOpenAi', or omitted (defaults to Azure OpenAI when keys are present).");
            }

            if (!useEchoClient)
            {
                string? endpoint = configuration["AzureOpenAI:Endpoint"];
                string? apiKey = configuration["AzureOpenAI:ApiKey"];
                string? deployment = configuration["AzureOpenAI:DeploymentName"];
                if (string.IsNullOrWhiteSpace(endpoint) ||
                    string.IsNullOrWhiteSpace(apiKey) ||
                    string.IsNullOrWhiteSpace(deployment))
                {
                    errors.Add(
                        "AgentExecution:Mode is 'Real' but one or more AzureOpenAI settings (Endpoint, ApiKey, DeploymentName) are missing.");
                }

                int maxCompletionTokens = configuration.GetValue("AzureOpenAI:MaxCompletionTokens", 0);

                if (maxCompletionTokens < 0 || maxCompletionTokens > 262_144)
                {
                    errors.Add(
                        "AzureOpenAI:MaxCompletionTokens must be between 1 and 262144 inclusive, or 0 / omitted to use the built-in default (4096).");
                }
            }
        }

        CollectLlmCompletionCacheErrors(configuration, errors);

        CollectSchemaFileErrors(configuration, errors);
        CollectBatchReplayErrors(configuration, errors);
        CollectApiDeprecationErrors(configuration, errors);
        CollectDataArchivalErrors(configuration, errors);
        CollectHostLeaderElectionErrors(configuration, errors);
        CollectRetrievalEmbeddingCapErrors(configuration, errors);
        CollectRetrievalVectorIndexErrors(configuration, errors);
        CollectRateLimitingErrors(configuration, errors);
        CollectHotPathCacheErrors(configuration, environment, errors);
        CollectBackgroundJobsErrors(configuration, errors);
        CollectOtlpObservabilityErrors(configuration, errors);
        CollectPrometheusObservabilityErrors(configuration, errors);
        CollectLlmTokenQuotaErrors(configuration, errors);

        if (!environment.IsProduction())

            return errors;


        ArchiForgeHostingRole hostingRole = HostingRoleResolver.Resolve(configuration);

        if (hostingRole == ArchiForgeHostingRole.Worker)
        {
            CollectProductionWebhookSecretErrors(configuration, errors);
            CollectProductionSqlRowLevelSecurityErrors(configuration, archiForge, errors);

            return errors;
        }

        CollectProductionCorsErrors(configuration, errors);
        CollectProductionWebhookSecretErrors(configuration, errors);
        CollectProductionSqlRowLevelSecurityErrors(configuration, archiForge, errors);

        string? authMode = ArchiForgeConfigurationBridge.ResolveAuthConfigurationValue(configuration, "Mode");
        if (string.Equals(authMode, "DevelopmentBypass", StringComparison.OrdinalIgnoreCase))

            errors.Add("ArchiForgeAuth:Mode (or ArchLucidAuth:Mode) cannot be DevelopmentBypass when the host environment is Production.");


        if (string.Equals(authMode, "JwtBearer", StringComparison.OrdinalIgnoreCase))

            if (string.IsNullOrWhiteSpace(ArchiForgeConfigurationBridge.ResolveAuthConfigurationValue(configuration, "Authority")))

                errors.Add(
                    "ArchiForgeAuth:Authority (or ArchLucidAuth:Authority) is required when auth Mode is JwtBearer in Production.");


        if (!string.Equals(authMode, "ApiKey", StringComparison.OrdinalIgnoreCase))
            return errors;

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

    /// <summary>Require RLS session context when using SQL in Production (API and Worker).</summary>
    private static void CollectProductionSqlRowLevelSecurityErrors(
        IConfiguration configuration,
        ArchiForgeOptions archiForge,
        List<string> errors)
    {
        if (!string.Equals(archiForge.StorageProvider, "Sql", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        SqlServerOptions sql =
            configuration.GetSection(SqlServerOptions.SectionName).Get<SqlServerOptions>() ?? new SqlServerOptions();

        if (sql.RowLevelSecurity.ApplySessionContext)
        {
            return;
        }

        errors.Add(
            "Production with ArchiForge:StorageProvider=Sql requires SqlServer:RowLevelSecurity:ApplySessionContext=true so tenant/workspace/project SESSION_CONTEXT keys are applied (defense in depth with SQL RLS).");
    }

    /// <summary>Fail-fast CORS checks in Production for API-facing hosts only.</summary>
    private static void CollectProductionCorsErrors(IConfiguration configuration, List<string> errors)
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

    }

    /// <summary>Outbound webhook HMAC when HTTP delivery is enabled (API and Worker).</summary>
    private static void CollectProductionWebhookSecretErrors(IConfiguration configuration, List<string> errors)
    {
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

    private static void CollectHostLeaderElectionErrors(IConfiguration configuration, List<string> errors)
    {
        bool enabled = configuration.GetValue("HostLeaderElection:Enabled", true);

        if (!enabled)
        {
            return;
        }

        int leaseSeconds = configuration.GetValue("HostLeaderElection:LeaseDurationSeconds", 90);

        if (leaseSeconds < 15 || leaseSeconds > 3600)
        {
            errors.Add(
                "HostLeaderElection:LeaseDurationSeconds must be between 15 and 3600 inclusive when HostLeaderElection:Enabled is true.");
        }

        int renewSeconds = configuration.GetValue("HostLeaderElection:RenewIntervalSeconds", 25);

        if (renewSeconds < 5)
        {
            errors.Add(
                "HostLeaderElection:RenewIntervalSeconds must be at least 5 when HostLeaderElection:Enabled is true.");
        }

        if (renewSeconds >= leaseSeconds)
        {
            errors.Add(
                "HostLeaderElection:RenewIntervalSeconds must be less than HostLeaderElection:LeaseDurationSeconds when HostLeaderElection:Enabled is true.");
        }

        int followerMs = configuration.GetValue("HostLeaderElection:FollowerPollMilliseconds", 2000);

        if (followerMs < 100 || followerMs > 120_000)
        {
            errors.Add(
                "HostLeaderElection:FollowerPollMilliseconds must be between 100 and 120000 when HostLeaderElection:Enabled is true.");
        }
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

    private static void CollectHotPathCacheErrors(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        List<string> errors)
    {
        HotPathCacheOptions opts =
            configuration.GetSection(HotPathCacheOptions.SectionName).Get<HotPathCacheOptions>() ??
            new HotPathCacheOptions();

        if (!opts.Enabled)
            return;

        string provider = opts.Provider ?? "Memory";

        if (!string.Equals(provider, "Memory", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(provider, "Redis", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(provider, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(
                "HotPathCache:Provider must be 'Memory', 'Redis', or 'Auto' when HotPathCache:Enabled is true.");
        }

        if (string.Equals(provider, "Redis", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(opts.RedisConnectionString))
        {
            errors.Add("HotPathCache:RedisConnectionString is required when HotPathCache:Provider is Redis.");
        }

        if (string.Equals(provider, "Auto", StringComparison.OrdinalIgnoreCase) &&
            opts.ExpectedApiReplicaCount > 1 &&
            string.IsNullOrWhiteSpace(opts.RedisConnectionString) &&
            !environment.IsDevelopment())
        {
            errors.Add(
                "HotPathCache:RedisConnectionString is required when HotPathCache:Provider is Auto and HotPathCache:ExpectedApiReplicaCount is greater than 1 outside Development (distributed cache across replicas).");
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
    /// Aligns with <see cref="ArchiForge.Host.Composition.ServiceCollectionExtensions.RegisterRetrieval"/>: only <c>InMemory</c> and <c>AzureSearch</c> are supported; omitted defaults to in-memory.
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

    private static void CollectBackgroundJobsErrors(IConfiguration configuration, List<string> errors)
    {
        BackgroundJobsOptions jobs =
            configuration.GetSection(BackgroundJobsOptions.SectionName).Get<BackgroundJobsOptions>() ??
            new BackgroundJobsOptions();

        if (!string.Equals(jobs.Mode, "Durable", StringComparison.OrdinalIgnoreCase))
            return;

        ArchiForgeOptions archi = ArchiForgeConfigurationBridge.ResolveArchiForgeOptions(configuration);

        if (!string.Equals(archi.StorageProvider, "Sql", StringComparison.OrdinalIgnoreCase))
            errors.Add("BackgroundJobs:Mode Durable requires ArchiForge:StorageProvider Sql (shared job state in SQL).");

        ArtifactLargePayloadOptions large =
            configuration.GetSection(ArtifactLargePayloadOptions.SectionName).Get<ArtifactLargePayloadOptions>() ??
            new ArtifactLargePayloadOptions();

        if (!string.Equals(large.BlobProvider, "AzureBlob", StringComparison.OrdinalIgnoreCase))
            errors.Add(
                "BackgroundJobs:Mode Durable requires ArtifactLargePayload:BlobProvider AzureBlob (queue + result blobs via managed identity).");

        if (string.IsNullOrWhiteSpace(large.AzureBlobServiceUri) && string.IsNullOrWhiteSpace(jobs.QueueServiceUri))
            errors.Add(
                "BackgroundJobs:Mode Durable requires BackgroundJobs:QueueServiceUri or ArtifactLargePayload:AzureBlobServiceUri.");

        if (string.IsNullOrWhiteSpace(jobs.ResultsContainerName))
            errors.Add("BackgroundJobs:ResultsContainerName must be set when BackgroundJobs:Mode is Durable.");

        int receiveBatch = configuration.GetValue("BackgroundJobs:ProcessorReceiveBatchSize", 16);

        if (receiveBatch < 1 || receiveBatch > 32)
        {
            errors.Add(
                "BackgroundJobs:ProcessorReceiveBatchSize must be between 1 and 32 when BackgroundJobs:Mode is Durable.");
        }
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

    private static void CollectOtlpObservabilityErrors(IConfiguration configuration, List<string> errors)
    {
        bool enabled = configuration.GetValue("Observability:Otlp:Enabled", false);

        if (!enabled)
            return;

        string? endpoint = configuration["Observability:Otlp:Endpoint"]?.Trim();

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            errors.Add(
                "Observability:Otlp:Enabled is true but Observability:Otlp:Endpoint is missing. Set a full OTLP base URL (gRPC or HTTP/protobuf per Observability:Otlp:Protocol).");
        }

        string? protocol = configuration["Observability:Otlp:Protocol"]?.Trim();

        if (string.IsNullOrWhiteSpace(protocol))
            return;

        if (!string.Equals(protocol, "Grpc", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(protocol, "HttpProtobuf", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(
                "Observability:Otlp:Protocol must be 'Grpc' or 'HttpProtobuf' when set.");
        }
    }

    private static void CollectPrometheusObservabilityErrors(IConfiguration configuration, List<string> errors)
    {
        bool enabled = configuration.GetValue("Observability:Prometheus:Enabled", false);

        if (!enabled)
            return;

        bool requireAuth = configuration.GetValue("Observability:Prometheus:RequireScrapeAuthentication", true);

        if (!requireAuth)
            return;

        string? user = configuration["Observability:Prometheus:ScrapeUsername"]?.Trim();
        string? password = configuration["Observability:Prometheus:ScrapePassword"];

        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
        {
            errors.Add(
                "Observability:Prometheus:Enabled is true and RequireScrapeAuthentication defaults to true: configure Observability:Prometheus:ScrapeUsername and ScrapePassword for scrape Basic auth, or set RequireScrapeAuthentication to false (acceptable only on trusted networks).");
        }
    }

    private static void CollectLlmCompletionCacheErrors(IConfiguration configuration, List<string> errors)
    {
        bool enabled = configuration.GetValue("LlmCompletionCache:Enabled", true);

        if (!enabled)
        {
            return;
        }

        int maxEntries = configuration.GetValue("LlmCompletionCache:MaxEntries", 256);

        if (maxEntries < 1 || maxEntries > 100_000)
        {
            errors.Add(
                "LlmCompletionCache:MaxEntries must be between 1 and 100000 when LlmCompletionCache:Enabled is true.");
        }

        int ttlSeconds = configuration.GetValue("LlmCompletionCache:AbsoluteExpirationSeconds", 600);

        if (ttlSeconds < 1 || ttlSeconds > 604_800)
        {
            errors.Add(
                "LlmCompletionCache:AbsoluteExpirationSeconds must be between 1 and 604800 when LlmCompletionCache:Enabled is true.");
        }

        string? provider = configuration["LlmCompletionCache:Provider"]?.Trim();

        if (!string.IsNullOrEmpty(provider) &&
            !string.Equals(provider, "Memory", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(provider, "Distributed", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("LlmCompletionCache:Provider must be 'Memory' or 'Distributed' when set.");
        }

        if (string.Equals(provider, "Distributed", StringComparison.OrdinalIgnoreCase))
        {
            string? llmRedis = configuration["LlmCompletionCache:RedisConnectionString"]?.Trim();
            string? hotRedis = configuration["HotPathCache:RedisConnectionString"]?.Trim();
            HotPathCacheOptions hotOpts =
                configuration.GetSection(HotPathCacheOptions.SectionName).Get<HotPathCacheOptions>() ??
                new HotPathCacheOptions();
            bool hotPathUsesRedis = hotOpts.Enabled &&
                                    string.Equals(
                                        HotPathCacheProviderResolver.ResolveEffectiveProvider(hotOpts),
                                        "Redis",
                                        StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(llmRedis) && string.IsNullOrEmpty(hotRedis) && !hotPathUsesRedis)
            {
                errors.Add(
                    "LlmCompletionCache:Provider Distributed requires LlmCompletionCache:RedisConnectionString, or HotPathCache:RedisConnectionString with HotPathCache configured for Redis, so the host can register IDistributedCache.");
            }
        }
    }

    private static void CollectLlmTokenQuotaErrors(IConfiguration configuration, List<string> errors)
    {
        bool enabled = configuration.GetValue("LlmTokenQuota:Enabled", false);

        if (!enabled)
        {
            return;
        }

        int windowMinutes = configuration.GetValue("LlmTokenQuota:WindowMinutes", 60);

        if (windowMinutes < 1 || windowMinutes > 1440)
        {
            errors.Add("LlmTokenQuota:WindowMinutes must be between 1 and 1440 when LlmTokenQuota:Enabled is true.");
        }

        long maxPrompt = configuration.GetValue<long>("LlmTokenQuota:MaxPromptTokensPerTenantPerWindow", 0);
        long maxCompletion = configuration.GetValue<long>("LlmTokenQuota:MaxCompletionTokensPerTenantPerWindow", 0);

        if (maxPrompt < 1 && maxCompletion < 1)
        {
            errors.Add(
                "When LlmTokenQuota:Enabled is true, set at least one of LlmTokenQuota:MaxPromptTokensPerTenantPerWindow or LlmTokenQuota:MaxCompletionTokensPerTenantPerWindow to a positive value.");
        }

        int assumedPrompt = configuration.GetValue("LlmTokenQuota:AssumedMaxPromptTokensPerRequest", 32_768);

        if (assumedPrompt < 1 || assumedPrompt > 1_000_000)
        {
            errors.Add(
                "LlmTokenQuota:AssumedMaxPromptTokensPerRequest must be between 1 and 1000000 when LlmTokenQuota:Enabled is true.");
        }

        int assumedCompletion = configuration.GetValue("LlmTokenQuota:AssumedMaxCompletionTokensPerRequest", 8_192);

        if (assumedCompletion < 1 || assumedCompletion > 262_144)
        {
            errors.Add(
                "LlmTokenQuota:AssumedMaxCompletionTokensPerRequest must be between 1 and 262144 when LlmTokenQuota:Enabled is true.");
        }
    }
}
