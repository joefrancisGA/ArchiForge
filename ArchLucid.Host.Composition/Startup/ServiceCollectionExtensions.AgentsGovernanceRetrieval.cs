using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.AgentRuntime;
using ArchLucid.AgentRuntime.Prompts;
using ArchLucid.AgentSimulator.Services;
using ArchLucid.Application.Governance;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Resilience;
using ArchLucid.Core.Scoping;
using ArchLucid.Host.Core.Configuration;
using ArchLucid.Host.Core.Resilience;
using ArchLucid.Host.Core.Services;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Retrieval.Chunking;
using ArchLucid.Retrieval.Embedding;
using ArchLucid.Retrieval.Indexing;
using ArchLucid.Retrieval.Queries;

using Microsoft.Extensions.Options;

using Polly;

namespace ArchLucid.Host.Composition.Startup;

public static partial class ServiceCollectionExtensions
{
    private static void RegisterAgentExecution(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AgentPromptCatalogOptions>(
            configuration.GetSection(AgentPromptCatalogOptions.SectionName));
        services.AddSingleton<IAgentSystemPromptCatalog, CachedAgentSystemPromptCatalog>();
        services.Configure<AgentExecutionResilienceOptions>(
            configuration.GetSection(AgentExecutionResilienceOptions.SectionName));
        services.PostConfigure<AgentExecutionResilienceOptions>(static o => o.Normalize());
        services.AddSingleton<IAgentHandlerConcurrencyGate, AgentHandlerConcurrencyGate>();
        services.AddSingleton<CircuitBreakerAuditBridge>();
        services.Configure<LlmTokenQuotaOptions>(configuration.GetSection(LlmTokenQuotaOptions.SectionName));
        services.Configure<LlmTelemetryOptions>(configuration.GetSection(LlmTelemetryOptions.SectionName));
        services.Configure<FallbackLlmOptions>(configuration.GetSection(FallbackLlmOptions.SectionName));

        string? agentMode = configuration["AgentExecution:Mode"];
        string? completionClientRaw = configuration["AgentExecution:CompletionClient"]?.Trim();
        bool useEchoClient = string.Equals(agentMode, "Real", StringComparison.OrdinalIgnoreCase)
                              && string.Equals(completionClientRaw, "Echo", StringComparison.OrdinalIgnoreCase);

        bool completionIsExplicitAzure = string.Equals(completionClientRaw, "AzureOpenAi", StringComparison.OrdinalIgnoreCase);
        bool azureKeysPresent =
            !string.IsNullOrWhiteSpace(configuration["AzureOpenAI:Endpoint"])
            && !string.IsNullOrWhiteSpace(configuration["AzureOpenAI:ApiKey"])
            && !string.IsNullOrWhiteSpace(configuration["AzureOpenAI:DeploymentName"]);

        bool useAzureOpenAi = !string.Equals(agentMode, "Simulator", StringComparison.OrdinalIgnoreCase)
                              && !useEchoClient
                              && (string.IsNullOrEmpty(completionClientRaw) || completionIsExplicitAzure)
                              && azureKeysPresent;

        ConfigureLlmTelemetryLabels(services, configuration, agentMode, useAzureOpenAi, useEchoClient);

        if (string.Equals(agentMode, "Simulator", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<DeterministicAgentSimulator>();
            services.AddScoped<IAgentExecutor, SimulatorExecutionTraceRecordingExecutor>();
            RegisterFakeAgentCompletionClient(services);
        }
        else
        {
            services.AddScoped<IAgentExecutor, RealAgentExecutor>();
            services.AddScoped<IAgentHandler, TopologyAgentHandler>();
            services.AddScoped<IAgentHandler, CostAgentHandler>();
            services.AddScoped<IAgentHandler, ComplianceAgentHandler>();
            services.AddScoped<IAgentHandler, CriticAgentHandler>();
            services.AddScoped<IAgentResultParser, AgentResultParser>();

            if (useEchoClient)
            {
                services.AddSingleton<LlmTokenQuotaWindowTracker>();
                RegisterEchoAgentCompletionPipeline(services);
            }
            else if (useAzureOpenAi)
            {
                FallbackLlmOptions fallbackOpts =
                    configuration.GetSection(FallbackLlmOptions.SectionName).Get<FallbackLlmOptions>()
                    ?? new FallbackLlmOptions();

                if (fallbackOpts.Enabled)
                {
                    if (string.IsNullOrWhiteSpace(fallbackOpts.Endpoint)
                        || string.IsNullOrWhiteSpace(fallbackOpts.ApiKey)
                        || string.IsNullOrWhiteSpace(fallbackOpts.DeploymentName))
                    {
                        throw new InvalidOperationException(
                            "ArchLucid:FallbackLlm is enabled but Endpoint, ApiKey, and DeploymentName must all be configured.");
                    }
                }

                bool fallbackLlmEnabled = fallbackOpts.Enabled;

                services.AddKeyedSingleton<CircuitBreakerGate>(
                    OpenAiCircuitBreakerKeys.Completion,
                    (sp, _) => CreateOpenAiCircuitBreakerGate(sp, OpenAiCircuitBreakerKeys.Completion));

                if (fallbackLlmEnabled)
                {
                    services.AddKeyedSingleton<CircuitBreakerGate>(
                        OpenAiCircuitBreakerKeys.CompletionFallback,
                        (sp, _) => CreateOpenAiCircuitBreakerGate(sp, OpenAiCircuitBreakerKeys.CompletionFallback));

                    services.AddSingleton<FallbackAzureOpenAiInnerClientHolder>(sp =>
                    {
                        FallbackLlmOptions fo = sp.GetRequiredService<IOptions<FallbackLlmOptions>>().Value;
                        IConfiguration cfg = sp.GetRequiredService<IConfiguration>();
                        int maxTokens = cfg.GetValue("AzureOpenAI:MaxCompletionTokens", 0);

                        if (maxTokens <= 0)
                        {
                            maxTokens = AzureOpenAiCompletionClient.DefaultMaxCompletionTokens;
                        }

                        AzureOpenAiCompletionClient client = new(
                            fo.Endpoint!,
                            fo.ApiKey!,
                            fo.DeploymentName!,
                            maxTokens);

                        return new FallbackAzureOpenAiInnerClientHolder(client);
                    });
                }

                services.AddSingleton<AzureOpenAiCompletionClient>(sp =>
                {
                    IConfiguration config = sp.GetRequiredService<IConfiguration>();
                    string endpoint = config["AzureOpenAI:Endpoint"]
                                      ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is missing.");
                    string apiKey = config["AzureOpenAI:ApiKey"]
                                    ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is missing.");
                    string deploymentName = config["AzureOpenAI:DeploymentName"]
                                            ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName is missing.");
                    int maxTokens = config.GetValue("AzureOpenAI:MaxCompletionTokens", 0);

                    if (maxTokens <= 0)
                    {
                        maxTokens = AzureOpenAiCompletionClient.DefaultMaxCompletionTokens;
                    }

                    return new AzureOpenAiCompletionClient(endpoint, apiKey, deploymentName, maxTokens);
                });

                services.AddSingleton<LlmTokenQuotaWindowTracker>();

                services.AddScoped<IAgentCompletionClient>(sp =>
                {
                    AzureOpenAiCompletionClient azureInner = sp.GetRequiredService<AzureOpenAiCompletionClient>();
                    IConfiguration config = sp.GetRequiredService<IConfiguration>();
                    string primaryDeployment = config["AzureOpenAI:DeploymentName"]
                        ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName is missing.");
                    CircuitBreakerGate primaryGate =
                        sp.GetRequiredKeyedService<CircuitBreakerGate>(OpenAiCircuitBreakerKeys.Completion);

                    IAgentCompletionClient primaryChain = BuildAzureOpenAiScopedCompletionChain(
                        sp,
                        azureInner,
                        primaryGate,
                        primaryDeployment);

                    if (!fallbackLlmEnabled)
                    {
                        return primaryChain;
                    }

                    FallbackAzureOpenAiInnerClientHolder holder = sp.GetRequiredService<FallbackAzureOpenAiInnerClientHolder>();
                    CircuitBreakerGate fallbackGate =
                        sp.GetRequiredKeyedService<CircuitBreakerGate>(OpenAiCircuitBreakerKeys.CompletionFallback);
                    FallbackLlmOptions fo = sp.GetRequiredService<IOptions<FallbackLlmOptions>>().Value;

                    IAgentCompletionClient secondaryChain = BuildAzureOpenAiScopedCompletionChain(
                        sp,
                        holder.Client,
                        fallbackGate,
                        fo.DeploymentName!);

                    ILogger<FallbackAgentCompletionClient> fallbackLogger =
                        sp.GetRequiredService<ILogger<FallbackAgentCompletionClient>>();

                    return new FallbackAgentCompletionClient(primaryChain, secondaryChain, fallbackLogger);
                });
            }
            else
            {
                RegisterFakeAgentCompletionClient(services);
            }
        }

        services.AddScoped<ILlmCompletionProvider>(sp =>
        {
            IAgentCompletionClient inner = sp.GetRequiredService<IAgentCompletionClient>();
            IOptionsMonitor<LlmTelemetryLabelOptions> labelOpts = sp.GetRequiredService<IOptionsMonitor<LlmTelemetryLabelOptions>>();
            LlmTelemetryLabelOptions labels = labelOpts.CurrentValue;

            return new DelegatingLlmCompletionProvider(inner, labels.ProviderId, labels.ModelDeploymentLabel);
        });

        services.AddScoped<ILlmProvider>(sp => sp.GetRequiredService<ILlmCompletionProvider>());
    }

    private static void ConfigureLlmTelemetryLabels(
        IServiceCollection services,
        IConfiguration configuration,
        string? agentMode,
        bool useAzureOpenAi,
        bool useEchoClient)
    {
        services.Configure<LlmTelemetryLabelOptions>(options =>
        {
            if (useEchoClient)
            {
                options.ProviderId = "echo";
                options.ModelDeploymentLabel = "echo";
            }
            else if (useAzureOpenAi)
            {
                options.ProviderId = "azure-openai";
                options.ModelDeploymentLabel = configuration["AzureOpenAI:DeploymentName"]?.Trim() ?? "unknown";
            }
            else if (string.Equals(agentMode, "Simulator", StringComparison.OrdinalIgnoreCase))
            {
                options.ProviderId = "simulator";
                options.ModelDeploymentLabel = "deterministic";
            }
            else
            {
                options.ProviderId = "fake";
                options.ModelDeploymentLabel = "fake";
            }
        });
    }

    private static void RegisterEchoAgentCompletionPipeline(IServiceCollection services)
    {
        services.AddScoped<IAgentCompletionClient>(sp =>
        {
            EchoAgentCompletionClient echoInner = new();
            LlmTokenQuotaWindowTracker quotaTracker = sp.GetRequiredService<LlmTokenQuotaWindowTracker>();
            IScopeContextProvider scopeProvider = sp.GetRequiredService<IScopeContextProvider>();
            IOptionsMonitor<LlmTokenQuotaOptions> quotaOpts = sp.GetRequiredService<IOptionsMonitor<LlmTokenQuotaOptions>>();
            IOptionsMonitor<LlmTelemetryOptions> telemetryOpts =
                sp.GetRequiredService<IOptionsMonitor<LlmTelemetryOptions>>();
            IOptionsMonitor<LlmTelemetryLabelOptions> labelTelemetryOpts =
                sp.GetRequiredService<IOptionsMonitor<LlmTelemetryLabelOptions>>();
            sp.GetRequiredService<ILogger<LlmCompletionAccountingClient>>();

            IAgentCompletionClient completionPipeline = new LlmCompletionAccountingClient(
                echoInner,
                quotaTracker,
                scopeProvider,
                quotaOpts,
                telemetryOpts,
                labelTelemetryOpts);

            IConfiguration config = sp.GetRequiredService<IConfiguration>();
            LlmCompletionResponseCacheOptions cacheOptions = config
                                                               .GetSection(LlmCompletionResponseCacheOptions.SectionName)
                                                               .Get<LlmCompletionResponseCacheOptions>()
                                                           ?? new LlmCompletionResponseCacheOptions();

            string cacheDeploymentLabel = config["AzureOpenAI:DeploymentName"]?.Trim() ?? "echo";

            if (!cacheOptions.Enabled)
                return completionPipeline;

            TimeSpan ttl = TimeSpan.FromSeconds(Math.Max(1, cacheOptions.AbsoluteExpirationSeconds));
            ILlmCompletionResponseStore store = sp.GetRequiredService<ILlmCompletionResponseStore>();
            ILogger<CachingAgentCompletionClient> cacheLogger =
                sp.GetRequiredService<ILogger<CachingAgentCompletionClient>>();
            completionPipeline = new CachingAgentCompletionClient(
                completionPipeline,
                store,
                cacheDeploymentLabel,
                enabled: true,
                partitionByScope: cacheOptions.PartitionByScope,
                absoluteExpiration: ttl,
                scopeProvider: scopeProvider,
                logger: cacheLogger);

            return completionPipeline;
        });
    }

    /// <summary>
    /// Ask/Explanation paths resolve <see cref="IAgentCompletionClient"/> even when
    /// <see cref="SimulatorExecutionTraceRecordingExecutor"/> wraps <see cref="DeterministicAgentSimulator"/> (no real agent handlers).
    /// </summary>
    private static void RegisterFakeAgentCompletionClient(IServiceCollection services)
    {
        JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        services.AddScoped<IAgentCompletionClient>(_ => new FakeAgentCompletionClient(
            (_, userPrompt) =>
            {
                string runId = "RUN-001";
                string taskId = "TASK-TOPO-001";

                foreach (string line in userPrompt.Split('\n'))
                {
                    ReadOnlySpan<char> span = line.AsSpan().Trim();

                    if (span.StartsWith("RunId:", StringComparison.OrdinalIgnoreCase))
                    {
                        runId = span.Length > 6 ? span[6..].Trim().ToString() : runId;
                    }
                    else if (span.StartsWith("TaskId:", StringComparison.OrdinalIgnoreCase))
                    {
                        taskId = span.Length > 7 ? span[7..].Trim().ToString() : taskId;
                    }
                }

                ArchitectureRequest dummyRequest = new()
                {
                    SystemName = "Default",
                    Description = "Default request for fake topology response.",
                    Environment = "prod"
                };
                AgentResult result = FakeScenarioFactory.CreateTopologyResult(runId, taskId, dummyRequest);

                return JsonSerializer.Serialize(result, jsonOptions);
            }));
    }

    private static void RegisterGovernance(IServiceCollection services, IConfiguration configuration)
    {
        ArchLucidOptions governanceStorage = ArchLucidConfigurationBridge.ResolveArchLucidOptions(configuration);

        if (ArchLucidOptions.EffectiveIsInMemory(governanceStorage.StorageProvider))
        {
            services.AddSingleton<IGovernanceApprovalRequestRepository, InMemoryGovernanceApprovalRequestRepository>();
            services.AddSingleton<IGovernancePromotionRecordRepository, InMemoryGovernancePromotionRecordRepository>();
            services.AddSingleton<IGovernanceEnvironmentActivationRepository, InMemoryGovernanceEnvironmentActivationRepository>();
        }
        else
        {
            services.AddScoped<IGovernanceApprovalRequestRepository, GovernanceApprovalRequestRepository>();
            services.AddScoped<IGovernancePromotionRecordRepository, GovernancePromotionRecordRepository>();
            services.AddScoped<IGovernanceEnvironmentActivationRepository, GovernanceEnvironmentActivationRepository>();
        }

        services.AddScoped<IGovernanceWorkflowService, GovernanceWorkflowService>();
        services.AddScoped<IGovernanceDashboardService, GovernanceDashboardService>();
        services.AddScoped<IGovernanceLineageService, GovernanceLineageService>();
        services.AddScoped<IGovernanceRationaleService, GovernanceRationaleService>();
        services.AddScoped<IComplianceDriftTrendService, ComplianceDriftTrendService>();
    }

    private static void RegisterRetrieval(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RetrievalEmbeddingCapOptions>(
            configuration.GetSection(RetrievalEmbeddingCapOptions.SectionName));

        services.AddSingleton<ITextChunker, SimpleTextChunker>();
        services.AddScoped<IRetrievalDocumentBuilder, RetrievalDocumentBuilder>();
        services.AddScoped<IRetrievalIndexingService, RetrievalIndexingService>();
        services.AddScoped<IRetrievalQueryService, RetrievalQueryService>();
        services.AddScoped<IRetrievalRunCompletionIndexer, RetrievalRunCompletionIndexer>();

        string vectorMode = configuration["Retrieval:VectorIndex"] ?? "InMemory";

        if (string.Equals(vectorMode, "AzureSearch", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IAzureSearchClient, NotConfiguredAzureSearchClient>();
            services.AddSingleton<IVectorIndex, AzureAiSearchVectorIndex>();
        }
        else
        {
            services.AddSingleton<IVectorIndex, InMemoryVectorIndex>();
        }

        string? embedDeployment = configuration["AzureOpenAI:EmbeddingDeploymentName"];
        string? endpoint = configuration["AzureOpenAI:Endpoint"];
        string? apiKey = configuration["AzureOpenAI:ApiKey"];
        bool useAzureEmbeddings = !string.IsNullOrWhiteSpace(embedDeployment)
                                  && !string.IsNullOrWhiteSpace(endpoint)
                                  && !string.IsNullOrWhiteSpace(apiKey);

        if (useAzureEmbeddings)
        {
            services.AddKeyedSingleton<CircuitBreakerGate>(
                OpenAiCircuitBreakerKeys.Embedding,
                (sp, _) => CreateOpenAiCircuitBreakerGate(sp, OpenAiCircuitBreakerKeys.Embedding));

            services.AddSingleton<IOpenAiEmbeddingClient>(sp =>
            {
                AzureOpenAiEmbeddingClient inner = new(endpoint!, apiKey!, embedDeployment!);
                CircuitBreakerGate gate = sp.GetRequiredKeyedService<CircuitBreakerGate>(OpenAiCircuitBreakerKeys.Embedding);
                ILogger<CircuitBreakingOpenAiEmbeddingClient> logger =
                    sp.GetRequiredService<ILogger<CircuitBreakingOpenAiEmbeddingClient>>();
                AgentExecutionResilienceOptions resOpts =
                    sp.GetRequiredService<IOptions<AgentExecutionResilienceOptions>>().Value;
                resOpts.Normalize();
                ResiliencePipeline embeddingRetry = LlmCallResilienceDefaults.BuildLlmRetryPipeline(
                    logger: logger,
                    maxRetryAttempts: resOpts.LlmCallMaxRetryAttempts,
                    baseDelay: TimeSpan.FromMilliseconds(resOpts.LlmCallBaseDelayMilliseconds),
                    maxDelay: TimeSpan.FromSeconds(resOpts.LlmCallMaxDelaySeconds),
                    gateName: OpenAiCircuitBreakerKeys.Embedding);

                return new CircuitBreakingOpenAiEmbeddingClient(inner, gate, embeddingRetry, logger);
            });
            services.AddSingleton<IEmbeddingService, AzureOpenAiEmbeddingService>();
        }
        else
        {
            services.AddSingleton<IEmbeddingService, FakeEmbeddingService>();
        }
    }

    private static void RegisterAzureOpenAiCircuitBreakerOptions(IServiceCollection services, IConfiguration configuration)
    {
        const string completionPath = "AzureOpenAI:CircuitBreaker:Completion";
        const string embeddingPath = "AzureOpenAI:CircuitBreaker:Embedding";
        const string sharedPath = "AzureOpenAI:CircuitBreaker";

        services.Configure<CircuitBreakerOptions>(
            OpenAiCircuitBreakerKeys.Completion,
            configuration.GetSection(completionPath));
        services.Configure<CircuitBreakerOptions>(
            OpenAiCircuitBreakerKeys.Embedding,
            configuration.GetSection(embeddingPath));
        services.Configure<CircuitBreakerOptions>(
            OpenAiCircuitBreakerKeys.CompletionFallback,
            configuration.GetSection(completionPath));

        services.PostConfigure<CircuitBreakerOptions>(
            OpenAiCircuitBreakerKeys.Completion,
            opts => ApplySharedOpenAiCircuitBreakerFallback(configuration, completionPath, sharedPath, opts));

        services.PostConfigure<CircuitBreakerOptions>(
            OpenAiCircuitBreakerKeys.Embedding,
            opts => ApplySharedOpenAiCircuitBreakerFallback(configuration, embeddingPath, sharedPath, opts));

        services.PostConfigure<CircuitBreakerOptions>(
            OpenAiCircuitBreakerKeys.CompletionFallback,
            opts => ApplySharedOpenAiCircuitBreakerFallback(configuration, completionPath, sharedPath, opts));
    }

    private static void ApplySharedOpenAiCircuitBreakerFallback(
        IConfiguration configuration,
        string perGateConfigurationPath,
        string sharedConfigurationPath,
        CircuitBreakerOptions options)
    {
        IConfigurationSection perGate = configuration.GetSection(perGateConfigurationPath);
        IConfigurationSection shared = configuration.GetSection(sharedConfigurationPath);

        if (string.IsNullOrEmpty(perGate["FailureThreshold"]))
        {
            int? fromShared = shared.GetValue<int?>("FailureThreshold");
            if (fromShared.HasValue)
            {
                options.FailureThreshold = fromShared.Value;
            }
        }

        if (string.IsNullOrEmpty(perGate["DurationOfBreakSeconds"]))
        {
            int? fromShared = shared.GetValue<int?>("DurationOfBreakSeconds");
            if (fromShared.HasValue)
            {
                options.DurationOfBreakSeconds = fromShared.Value;
            }
        }

        options.ApplyDefaults();
    }

    private static CircuitBreakerGate CreateOpenAiCircuitBreakerGate(IServiceProvider serviceProvider, string gateName)
    {
        IOptionsMonitor<CircuitBreakerOptions> monitor =
            serviceProvider.GetRequiredService<IOptionsMonitor<CircuitBreakerOptions>>();

        CircuitBreakerAuditBridge? bridge = serviceProvider.GetService<CircuitBreakerAuditBridge>();
        Action<CircuitBreakerAuditEntry>? onAudit = bridge?.CreateCallback();

        return new CircuitBreakerGate(gateName, monitor, onAuditEntry: onAudit);
    }

    private sealed class FallbackAzureOpenAiInnerClientHolder(AzureOpenAiCompletionClient client)
    {
        public AzureOpenAiCompletionClient Client { get; } =
            client ?? throw new ArgumentNullException(nameof(client));
    }

    private static IAgentCompletionClient BuildAzureOpenAiScopedCompletionChain(
        IServiceProvider sp,
        AzureOpenAiCompletionClient azureInner,
        CircuitBreakerGate gate,
        string cachingDeploymentLabel)
    {
        LlmTokenQuotaWindowTracker quotaTracker = sp.GetRequiredService<LlmTokenQuotaWindowTracker>();
        IScopeContextProvider scopeProvider = sp.GetRequiredService<IScopeContextProvider>();
        IOptionsMonitor<LlmTokenQuotaOptions> quotaOpts = sp.GetRequiredService<IOptionsMonitor<LlmTokenQuotaOptions>>();
        IOptionsMonitor<LlmTelemetryOptions> telemetryOpts =
            sp.GetRequiredService<IOptionsMonitor<LlmTelemetryOptions>>();
        IOptionsMonitor<LlmTelemetryLabelOptions> labelTelemetryOpts =
            sp.GetRequiredService<IOptionsMonitor<LlmTelemetryLabelOptions>>();
        sp.GetRequiredService<ILogger<LlmCompletionAccountingClient>>();

        IAgentCompletionClient completionPipeline = new LlmCompletionAccountingClient(
            azureInner,
            quotaTracker,
            scopeProvider,
            quotaOpts,
            telemetryOpts,
            labelTelemetryOpts);

        IConfiguration config = sp.GetRequiredService<IConfiguration>();
        LlmCompletionResponseCacheOptions cacheOptions = config
                                                           .GetSection(LlmCompletionResponseCacheOptions.SectionName)
                                                           .Get<LlmCompletionResponseCacheOptions>()
                                                       ?? new LlmCompletionResponseCacheOptions();

        if (cacheOptions.Enabled)
        {
            TimeSpan ttl = TimeSpan.FromSeconds(Math.Max(1, cacheOptions.AbsoluteExpirationSeconds));
            ILlmCompletionResponseStore store = sp.GetRequiredService<ILlmCompletionResponseStore>();
            ILogger<CachingAgentCompletionClient> cacheLogger =
                sp.GetRequiredService<ILogger<CachingAgentCompletionClient>>();
            completionPipeline = new CachingAgentCompletionClient(
                completionPipeline,
                store,
                cachingDeploymentLabel,
                enabled: true,
                partitionByScope: cacheOptions.PartitionByScope,
                absoluteExpiration: ttl,
                scopeProvider: scopeProvider,
                logger: cacheLogger);
        }

        ILogger<CircuitBreakingAgentCompletionClient> logger =
            sp.GetRequiredService<ILogger<CircuitBreakingAgentCompletionClient>>();
        AgentExecutionResilienceOptions resOpts =
            sp.GetRequiredService<IOptions<AgentExecutionResilienceOptions>>().Value;
        resOpts.Normalize();
        ResiliencePipeline llmRetry = LlmCallResilienceDefaults.BuildLlmRetryPipeline(
            logger: logger,
            maxRetryAttempts: resOpts.LlmCallMaxRetryAttempts,
            baseDelay: TimeSpan.FromMilliseconds(resOpts.LlmCallBaseDelayMilliseconds),
            maxDelay: TimeSpan.FromSeconds(resOpts.LlmCallMaxDelaySeconds),
            gateName: gate.GateName);

        return new CircuitBreakingAgentCompletionClient(completionPipeline, gate, llmRetry, logger);
    }
}
