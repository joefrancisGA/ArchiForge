using System.Text.Json;
using System.Text.Json.Serialization;

using ArchiForge.AgentRuntime;
using ArchiForge.AgentSimulator.Services;
using ArchiForge.Application.Governance;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Requests;
using ArchiForge.Core.Configuration;
using ArchiForge.Core.Resilience;
using ArchiForge.Core.Scoping;
using ArchiForge.Host.Core.Configuration;
using ArchiForge.Host.Core.Resilience;
using ArchiForge.Persistence.Data.Repositories;
using ArchiForge.Retrieval.Chunking;
using ArchiForge.Retrieval.Embedding;
using ArchiForge.Retrieval.Indexing;
using ArchiForge.Retrieval.Queries;

using Microsoft.Extensions.Options;

namespace ArchiForge.Host.Composition.Startup;

public static partial class ServiceCollectionExtensions
{
    private static void RegisterAgentExecution(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AgentPromptCatalogOptions>(
            configuration.GetSection(AgentPromptCatalogOptions.SectionName));
        services.Configure<LlmTokenQuotaOptions>(configuration.GetSection(LlmTokenQuotaOptions.SectionName));
        services.Configure<LlmTelemetryOptions>(configuration.GetSection(LlmTelemetryOptions.SectionName));

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
                services.AddKeyedSingleton<CircuitBreakerGate>(
                    OpenAiCircuitBreakerKeys.Completion,
                    (sp, _) => new CircuitBreakerGate(ResolveOpenAiCircuitBreakerOptions(sp.GetRequiredService<IConfiguration>())));

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
                    LlmTokenQuotaWindowTracker quotaTracker = sp.GetRequiredService<LlmTokenQuotaWindowTracker>();
                    IScopeContextProvider scopeProvider = sp.GetRequiredService<IScopeContextProvider>();
                    IOptionsMonitor<LlmTokenQuotaOptions> quotaOpts = sp.GetRequiredService<IOptionsMonitor<LlmTokenQuotaOptions>>();
                    IOptionsMonitor<LlmTelemetryOptions> telemetryOpts =
                        sp.GetRequiredService<IOptionsMonitor<LlmTelemetryOptions>>();
                    IOptionsMonitor<LlmTelemetryLabelOptions> labelTelemetryOpts =
                        sp.GetRequiredService<IOptionsMonitor<LlmTelemetryLabelOptions>>();
                    ILogger<LlmCompletionAccountingClient> accountingLogger =
                        sp.GetRequiredService<ILogger<LlmCompletionAccountingClient>>();

                    IAgentCompletionClient completionPipeline = new LlmCompletionAccountingClient(
                        azureInner,
                        quotaTracker,
                        scopeProvider,
                        quotaOpts,
                        telemetryOpts,
                        labelTelemetryOpts,
                        accountingLogger);

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
                            config["AzureOpenAI:DeploymentName"]
                            ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName is missing."),
                            enabled: true,
                            partitionByScope: cacheOptions.PartitionByScope,
                            absoluteExpiration: ttl,
                            scopeProvider: scopeProvider,
                            logger: cacheLogger);
                    }

                    CircuitBreakerGate gate = sp.GetRequiredKeyedService<CircuitBreakerGate>(OpenAiCircuitBreakerKeys.Completion);
                    ILogger<CircuitBreakingAgentCompletionClient> logger =
                        sp.GetRequiredService<ILogger<CircuitBreakingAgentCompletionClient>>();

                    return new CircuitBreakingAgentCompletionClient(completionPipeline, gate, logger);
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
            ILogger<LlmCompletionAccountingClient> accountingLogger =
                sp.GetRequiredService<ILogger<LlmCompletionAccountingClient>>();

            IAgentCompletionClient completionPipeline = new LlmCompletionAccountingClient(
                echoInner,
                quotaTracker,
                scopeProvider,
                quotaOpts,
                telemetryOpts,
                labelTelemetryOpts,
                accountingLogger);

            IConfiguration config = sp.GetRequiredService<IConfiguration>();
            LlmCompletionResponseCacheOptions cacheOptions = config
                                                               .GetSection(LlmCompletionResponseCacheOptions.SectionName)
                                                               .Get<LlmCompletionResponseCacheOptions>()
                                                           ?? new LlmCompletionResponseCacheOptions();

            string cacheDeploymentLabel = config["AzureOpenAI:DeploymentName"]?.Trim() ?? "echo";

            if (cacheOptions.Enabled)
            {
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
            }

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
        ArchiForgeOptions governanceStorage = ArchiForgeConfigurationBridge.ResolveArchiForgeOptions(configuration);

        if (string.Equals(governanceStorage.StorageProvider, "InMemory", StringComparison.OrdinalIgnoreCase))
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
                (sp, _) => new CircuitBreakerGate(ResolveOpenAiCircuitBreakerOptions(sp.GetRequiredService<IConfiguration>())));

            services.AddSingleton<IOpenAiEmbeddingClient>(sp =>
            {
                AzureOpenAiEmbeddingClient inner = new(endpoint!, apiKey!, embedDeployment!);
                CircuitBreakerGate gate = sp.GetRequiredKeyedService<CircuitBreakerGate>(OpenAiCircuitBreakerKeys.Embedding);
                ILogger<CircuitBreakingOpenAiEmbeddingClient> logger =
                    sp.GetRequiredService<ILogger<CircuitBreakingOpenAiEmbeddingClient>>();

                return new CircuitBreakingOpenAiEmbeddingClient(inner, gate, logger);
            });
            services.AddSingleton<IEmbeddingService, AzureOpenAiEmbeddingService>();
        }
        else
        {
            services.AddSingleton<IEmbeddingService, FakeEmbeddingService>();
        }
    }

    private static CircuitBreakerOptions ResolveOpenAiCircuitBreakerOptions(IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection("AzureOpenAI:CircuitBreaker");
        CircuitBreakerOptions options = new()
        {
            FailureThreshold = section.GetValue<int?>("FailureThreshold") ?? CircuitBreakerOptions.DefaultFailureThreshold,
            DurationOfBreakSeconds = section.GetValue<int?>("DurationOfBreakSeconds")
                                     ?? CircuitBreakerOptions.DefaultDurationOfBreakSeconds
        };
        options.ApplyDefaults();

        return options;
    }
}
