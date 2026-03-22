using ArchiForge.AgentRuntime;
using ArchiForge.AgentSimulator.Services;
using ArchiForge.Api.Health;
using ArchiForge.Api.Jobs;
using ArchiForge.Api.Services;
using ArchiForge.Api.Startup.Validation;
using ArchiForge.Application;
using ArchiForge.Application.Agents;
using ArchiForge.Application.Analysis;
using ArchiForge.Application.Determinism;
using ArchiForge.Application.Diagrams;
using ArchiForge.Application.Diffs;
using ArchiForge.Application.Evidence;
using ArchiForge.Application.Exports;
using ArchiForge.Application.Summaries;
using ArchiForge.ArtifactSynthesis.Docx;
using ArchiForge.ArtifactSynthesis.Generators;
using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Packaging;
using ArchiForge.ArtifactSynthesis.Renderers;
using ArchiForge.ArtifactSynthesis.Services;
using ArchiForge.Contracts.Requests;
using ArchiForge.Coordinator.Services;
using ArchiForge.AgentRuntime.Explanation;
using ArchiForge.Api.Ask;
using ArchiForge.Api.Hosted;
using ArchiForge.Api.Services.Ask;
using ArchiForge.Api.Services.Delivery;
using ArchiForge.Core.Ask;
using ArchiForge.Decisioning.Advisory.Analysis;
using ArchiForge.Decisioning.Advisory.Delivery;
using ArchiForge.Decisioning.Advisory.Learning;
using ArchiForge.Decisioning.Alerts;
using ArchiForge.Decisioning.Alerts.Composite;
using ArchiForge.Decisioning.Alerts.Simulation;
using ArchiForge.Decisioning.Alerts.Delivery;
using ArchiForge.Decisioning.Advisory.Scheduling;
using ArchiForge.Decisioning.Advisory.Services;
using ArchiForge.Decisioning.Comparison;
using ArchiForge.Data.Infrastructure;
using ArchiForge.Data.Repositories;
using ArchiForge.DecisionEngine.Services;
using ArchiForge.DecisionEngine.Validation;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using ArchiForge.ContextIngestion.Canonicalization;
using ArchiForge.ContextIngestion.Connectors;
using ArchiForge.ContextIngestion.Contracts;
using ArchiForge.ContextIngestion.Infrastructure;
using ArchiForge.ContextIngestion.Parsing;
using ArchiForge.ContextIngestion.Summaries;
using ContextConnector = ArchiForge.ContextIngestion.Interfaces.IContextConnector;
using ContextIngestionService = ArchiForge.ContextIngestion.Interfaces.IContextIngestionService;
using ArchiForge.KnowledgeGraph.Inference;
using ArchiForge.KnowledgeGraph.Mapping;
using ArchiForge.KnowledgeGraph.Interfaces;
using ArchiForge.KnowledgeGraph.Services;
using GraphBuilder = ArchiForge.KnowledgeGraph.Interfaces.IGraphBuilder;
using KnowledgeGraphService = ArchiForge.KnowledgeGraph.Interfaces.IKnowledgeGraphService;
using ArchiForge.Retrieval.Chunking;
using ArchiForge.Retrieval.Embedding;
using ArchiForge.Retrieval.Indexing;
using ArchiForge.Persistence.Advisory;
using ArchiForge.Persistence.Alerts;
using ArchiForge.Persistence.Alerts.Simulation;
using ArchiForge.Retrieval.Queries;

namespace ArchiForge.Api.Startup;

internal static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddArchiForgeApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddArchiForgeStorage(configuration);
        RegisterAdvisoryScheduling(services);
        RegisterDigestDelivery(services);
        RegisterAlerts(services);
        RegisterDataInfrastructure(services);
        RegisterBackgroundJobs(services);
        RegisterRunExportAndArchitectureAnalysis(services, configuration);
        RegisterComparisonReplayAndDrift(services);
        RegisterRunReplayManifestAndDiffs(services);
        RegisterContextIngestionAndKnowledgeGraph(services);
        RegisterDecisioningEngines(services);
        RegisterCoordinatorDecisionEngineAndRepositories(services, configuration);
        RegisterArtifactSynthesis(services);
        RegisterAgentExecution(services, configuration);
        RegisterRetrieval(services, configuration);
        services.AddScoped<ArchitectureRunOrchestrator>();
        return services;
    }

    private static void RegisterAdvisoryScheduling(IServiceCollection services)
    {
        services.AddScoped<IScanScheduleCalculator, SimpleScanScheduleCalculator>();
        services.AddScoped<IArchitectureDigestBuilder, ArchitectureDigestBuilder>();
        services.AddScoped<IAdvisoryScanRunner, AdvisoryScanRunner>();
        services.AddHostedService<AdvisoryScanHostedService>();
    }

    private static void RegisterDigestDelivery(IServiceCollection services)
    {
        services.AddSingleton<IEmailSender, FakeEmailSender>();
        services.AddSingleton<IWebhookPoster, FakeWebhookPoster>();
        services.AddScoped<IDigestDeliveryChannel, DigestEmailDeliveryChannel>();
        services.AddScoped<IDigestDeliveryChannel, DigestTeamsWebhookDeliveryChannel>();
        services.AddScoped<IDigestDeliveryChannel, DigestSlackWebhookDeliveryChannel>();
        services.AddScoped<IDigestDeliveryDispatcher, DigestDeliveryDispatcher>();
    }

    private static void RegisterAlerts(IServiceCollection services)
    {
        services.AddScoped<IAlertEvaluator, AlertEvaluator>();
        services.AddScoped<IAlertDeliveryChannel, AlertEmailDeliveryChannel>();
        services.AddScoped<IAlertDeliveryChannel, AlertTeamsWebhookDeliveryChannel>();
        services.AddScoped<IAlertDeliveryChannel, AlertSlackWebhookDeliveryChannel>();
        services.AddScoped<IAlertDeliveryChannel, AlertOnCallWebhookDeliveryChannel>();
        services.AddScoped<IAlertDeliveryDispatcher, AlertDeliveryDispatcher>();
        services.AddScoped<IAlertService, AlertService>();

        services.AddScoped<IAlertMetricSnapshotBuilder, AlertMetricSnapshotBuilder>();
        services.AddScoped<ICompositeAlertRuleEvaluator, CompositeAlertRuleEvaluator>();
        services.AddScoped<IAlertSuppressionPolicy, AlertSuppressionPolicy>();
        services.AddScoped<ICompositeAlertService, CompositeAlertService>();

        services.AddScoped<IAlertSimulationContextProvider, AlertSimulationContextProvider>();
        services.AddScoped<IRuleSimulationService, RuleSimulationService>();
    }

    private static void RegisterDataInfrastructure(IServiceCollection services)
    {
        services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
        services.AddHealthChecks()
            .AddCheck<SqlConnectionHealthCheck>("database", failureStatus: HealthStatus.Unhealthy);
    }

    private static void RegisterBackgroundJobs(IServiceCollection services)
    {
        services.AddSingleton<IBackgroundJobQueue, InMemoryBackgroundJobQueue>();
        services.AddHostedService(sp => (InMemoryBackgroundJobQueue)sp.GetRequiredService<IBackgroundJobQueue>());
    }

    private static void RegisterRunExportAndArchitectureAnalysis(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IRunExportRecordRepository, RunExportRecordRepository>();
        services.AddScoped<IRunExportAuditService, RunExportAuditService>();
        services.AddScoped<IArchitectureApplicationService, ArchitectureApplicationService>();
        services.AddScoped<IArchitectureAnalysisService, ArchitectureAnalysisService>();
        services.AddScoped<IArchitectureAnalysisExportService, MarkdownArchitectureAnalysisExportService>();
        services.AddScoped<IDiagramImageRenderer, NullDiagramImageRenderer>();
        services.AddScoped<IArchitectureAnalysisDocxExportService, DocxArchitectureAnalysisExportService>();
        services.Configure<ConsultingDocxTemplateOptions>(configuration.GetSection("ConsultingDocxTemplate"));
        services.AddScoped<IConsultingDocxTemplateOptionsProvider, DefaultConsultingDocxTemplateOptionsProvider>();
        services.AddScoped<IDocumentLogoProvider, FileSystemDocumentLogoProvider>();
        services.AddScoped<IArchitectureAnalysisConsultingDocxExportService, ConsultingDocxArchitectureAnalysisExportService>();
        services.AddSingleton<IConsultingDocxTemplateProfileResolver, DefaultConsultingDocxTemplateProfileResolver>();
        services.AddScoped<IConsultingDocxTemplateRecommendationService, ConsultingDocxTemplateRecommendationService>();
        services.AddScoped<IConsultingDocxExportProfileSelector, ConsultingDocxExportProfileSelector>();
        services.AddScoped<IEndToEndReplayComparisonService, EndToEndReplayComparisonService>();
        services.AddScoped<IEndToEndReplayComparisonSummaryFormatter, MarkdownEndToEndReplayComparisonSummaryFormatter>();
        services.AddScoped<IEndToEndReplayComparisonExportService, EndToEndReplayComparisonExportService>();
    }

    private static void RegisterComparisonReplayAndDrift(IServiceCollection services)
    {
        services.AddScoped<IComparisonRecordRepository, ComparisonRecordRepository>();
        services.AddScoped<IComparisonAuditService, ComparisonAuditService>();
        services.AddScoped<IComparisonDriftAnalyzer, ComparisonDriftAnalyzer>();
        services.AddScoped<IComparisonReplayService, ComparisonReplayService>();
        services.AddScoped<IComparisonReplayApiService, ComparisonReplayApiService>();
        services.AddScoped<IComparisonDriftReportExportService, ComparisonDriftReportExportService>();
        services.AddSingleton<IReplayDiagnosticsRecorder, ReplayDiagnosticsRecorder>();
    }

    private static void RegisterRunReplayManifestAndDiffs(IServiceCollection services)
    {
        services.AddScoped<IArchitectureRunService, ArchitectureRunService>();
        services.AddScoped<IReplayRunService, ReplayRunService>();
        services.AddScoped<IDeterminismCheckService, DeterminismCheckService>();
        services.AddScoped<IExportReplayService, ExportReplayService>();
        services.AddScoped<IAgentExecutorResolver, DefaultAgentExecutorResolver>();
        services.AddScoped<IDiagramGenerator, MermaidDiagramGenerator>();
        services.AddScoped<IManifestDiagramService, ManifestDiagramService>();
        services.AddScoped<IEvidenceSummaryFormatter, MarkdownEvidenceSummaryFormatter>();
        services.AddScoped<IManifestSummaryGenerator, MarkdownManifestSummaryGenerator>();
        services.AddScoped<IManifestSummaryService, ManifestSummaryService>();
        services.AddScoped<IArchitectureExportService, MarkdownArchitectureExportService>();
        services.AddScoped<IManifestDiffService, ManifestDiffService>();
        services.AddScoped<IManifestDiffSummaryFormatter, MarkdownManifestDiffSummaryFormatter>();
        services.AddScoped<IManifestDiffExportService, MarkdownManifestDiffExportService>();
        services.AddScoped<IAgentResultDiffService, AgentResultDiffService>();
        services.AddScoped<IAgentResultDiffSummaryFormatter, MarkdownAgentResultDiffSummaryFormatter>();
        services.AddScoped<IExportRecordDiffService, ExportRecordDiffService>();
        services.AddScoped<IExportRecordDiffSummaryFormatter, MarkdownExportRecordDiffSummaryFormatter>();
        services.AddScoped<IExportRecordDiffExportService, ExportRecordDiffExportService>();
        services.AddScoped<IDriftReportFormatter, MarkdownDriftReportFormatter>();
        services.AddScoped<DriftReportDocxExport>();
    }

    private static void RegisterContextIngestionAndKnowledgeGraph(IServiceCollection services)
    {
        services.AddSingleton<IContextDocumentParser, PlainTextContextDocumentParser>();

        services.AddSingleton<IInfrastructureDeclarationParser, JsonInfrastructureDeclarationParser>();
        services.AddSingleton<IInfrastructureDeclarationParser, SimpleTerraformDeclarationParser>();

        // Concrete connectors (registered once each).
        services.AddSingleton<StaticRequestContextConnector>();
        services.AddSingleton<InlineRequirementsConnector>();
        services.AddSingleton<PolicyReferenceConnector>();
        services.AddSingleton<TopologyHintsConnector>();
        services.AddSingleton<SecurityBaselineHintsConnector>();
        services.AddSingleton<DocumentConnector>();
        services.AddSingleton<InfrastructureDeclarationConnector>();

        // Fixed pipeline order (affects concatenated delta summaries and mental model). See docs/CONTEXT_INGESTION.md.
        services.AddSingleton<IEnumerable<ContextConnector>>(sp => new ContextConnector[]
        {
            sp.GetRequiredService<StaticRequestContextConnector>(),
            sp.GetRequiredService<InlineRequirementsConnector>(),
            sp.GetRequiredService<DocumentConnector>(),
            sp.GetRequiredService<PolicyReferenceConnector>(),
            sp.GetRequiredService<TopologyHintsConnector>(),
            sp.GetRequiredService<SecurityBaselineHintsConnector>(),
            sp.GetRequiredService<InfrastructureDeclarationConnector>(),
        });

        services.AddSingleton<ICanonicalEnricher, CanonicalInfrastructureEnricher>();
        services.AddSingleton<ICanonicalDeduplicator, CanonicalDeduplicator>();
        services.AddSingleton<IContextDeltaSummaryBuilder, DefaultContextDeltaSummaryBuilder>();

        services.AddScoped<ContextIngestionService, ArchiForge.ContextIngestion.Services.ContextIngestionService>();
        services.AddScoped<IGraphNodeFactory, GraphNodeFactory>();
        services.AddScoped<IGraphEdgeInferer, DefaultGraphEdgeInferer>();
        services.AddSingleton<IGraphValidator, GraphValidator>();
        services.AddScoped<GraphBuilder, KnowledgeGraph.Builders.DefaultGraphBuilder>();
        services.AddScoped<KnowledgeGraphService, ArchiForge.KnowledgeGraph.Services.KnowledgeGraphService>();
    }

    private static void RegisterCoordinatorDecisionEngineAndRepositories(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ICoordinatorService, CoordinatorService>();
        services.AddSchemaValidation(configuration);
        services.AddScoped<IDecisionEngineService, DecisionEngineService>();
        services.AddScoped<IDecisionEngineV2, DecisionEngineV2>();
        services.AddSingleton<IComparisonService, ComparisonService>();
        services.AddSingleton<IImprovementSignalAnalyzer, ImprovementSignalAnalyzer>();
        services.AddSingleton<IAdaptiveRecommendationScorer, AdaptiveRecommendationScorer>();
        services.AddSingleton<IRecommendationLearningAnalyzer, RecommendationLearningAnalyzer>();
        services.AddSingleton<IRecommendationGenerator, RecommendationGenerator>();
        services.AddScoped<IImprovementAdvisorService, ImprovementAdvisorService>();
        services.AddScoped<IExplanationService, ExplanationService>();
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IAskService, AskService>();
        services.AddScoped<Application.Decisions.IAgentEvaluationService, Application.Decisions.DefaultAgentEvaluationService>();
        services.AddScoped<IAgentEvaluationRepository, AgentEvaluationRepository>();
        services.AddScoped<IDecisionNodeRepository, DecisionNodeRepository>();
        services.AddScoped<IEvidenceBuilder, DefaultEvidenceBuilder>();
        services.AddScoped<IArchitectureRequestRepository, ArchitectureRequestRepository>();
        services.AddScoped<IArchitectureRunRepository, ArchitectureRunRepository>();
        services.AddScoped<IAgentTaskRepository, AgentTaskRepository>();
        services.AddScoped<IAgentResultRepository, AgentResultRepository>();
        services.AddScoped<IGoldenManifestRepository, GoldenManifestRepository>();
        services.AddScoped<IEvidenceBundleRepository, EvidenceBundleRepository>();
        services.AddScoped<IDecisionTraceRepository, DecisionTraceRepository>();
        services.AddScoped<IAgentEvidencePackageRepository, AgentEvidencePackageRepository>();
        services.AddScoped<IAgentExecutionTraceRepository, AgentExecutionTraceRepository>();
        services.AddScoped<IAgentExecutionTraceRecorder, AgentExecutionTraceRecorder>();
        services.AddHostedService<ConfigurationValidator>();
    }

    private static void RegisterArtifactSynthesis(IServiceCollection services)
    {
        services.AddSingleton<IArtifactContentTypeResolver, ArtifactContentTypeResolver>();
        services.AddSingleton<IArtifactPackagingService, ArtifactPackagingService>();
        services.AddSingleton<IArtifactBundleValidator, ArtifactBundleValidator>();
        services.AddSingleton<IDiagramRenderer, MermaidDiagramRenderer>();
        services.AddScoped<IArtifactGenerator, ReferenceArchitectureMarkdownGenerator>();
        services.AddScoped<IArtifactGenerator, ArchitectureNarrativeArtifactGenerator>();
        services.AddScoped<IArtifactGenerator, ComplianceMatrixArtifactGenerator>();
        services.AddScoped<IArtifactGenerator, CoverageSummaryArtifactGenerator>();
        services.AddScoped<IArtifactGenerator, DiagramAstGenerator>();
        services.AddScoped<IArtifactGenerator, MermaidDiagramArtifactGenerator>();
        services.AddScoped<IArtifactGenerator, InventoryArtifactGenerator>();
        services.AddScoped<IArtifactGenerator, CostSummaryArtifactGenerator>();
        services.AddScoped<IArtifactGenerator, UnresolvedIssuesArtifactGenerator>();
        services.AddScoped<IArtifactSynthesisService, ArtifactSynthesisService>();
        services.AddScoped<IDocxExportService, DocxExportService>();
    }

    private static void RegisterAgentExecution(IServiceCollection services, IConfiguration configuration)
    {
        var agentMode = configuration["AgentExecution:Mode"];
        if (string.Equals(agentMode, "Simulator", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IAgentExecutor, DeterministicAgentSimulator>();
        }
        else
        {
            services.AddScoped<IAgentExecutor, RealAgentExecutor>();
            services.AddScoped<IAgentHandler, TopologyAgentHandler>();
            services.AddScoped<IAgentHandler, CostAgentHandler>();
            services.AddScoped<IAgentHandler, ComplianceAgentHandler>();
            services.AddScoped<IAgentHandler, CriticAgentHandler>();
            services.AddScoped<IAgentResultParser, AgentResultParser>();

            var azureOpenAiEndpoint = configuration["AzureOpenAI:Endpoint"];
            var azureOpenAiKey = configuration["AzureOpenAI:ApiKey"];
            var azureOpenAiDeployment = configuration["AzureOpenAI:DeploymentName"];
            var useAzureOpenAi = !string.IsNullOrWhiteSpace(azureOpenAiEndpoint)
                && !string.IsNullOrWhiteSpace(azureOpenAiKey)
                && !string.IsNullOrWhiteSpace(azureOpenAiDeployment);

            if (useAzureOpenAi)
            {
                services.AddSingleton<IAgentCompletionClient>(sp =>
                {
                    var config = sp.GetRequiredService<IConfiguration>();
                    var endpoint = config["AzureOpenAI:Endpoint"]
                        ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is missing.");
                    var apiKey = config["AzureOpenAI:ApiKey"]
                        ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is missing.");
                    var deploymentName = config["AzureOpenAI:DeploymentName"]
                        ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName is missing.");
                    return new AzureOpenAiCompletionClient(endpoint, apiKey, deploymentName);
                });
            }
            else
            {
                var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
                services.AddScoped<IAgentCompletionClient>(_ => new FakeAgentCompletionClient(
                    (_, userPrompt) =>
                    {
                        var runId = "RUN-001";
                        var taskId = "TASK-TOPO-001";
                        foreach (var line in userPrompt.Split('\n'))
                        {
                            var span = line.AsSpan().Trim();
                            if (span.StartsWith("RunId:", StringComparison.OrdinalIgnoreCase))
                                runId = span.Length > 6 ? span[6..].Trim().ToString() : runId;
                            else if (span.StartsWith("TaskId:", StringComparison.OrdinalIgnoreCase))
                                taskId = span.Length > 7 ? span[7..].Trim().ToString() : taskId;
                        }
                        var dummyRequest = new ArchitectureRequest
                        {
                            SystemName = "Default",
                            Description = "Default request for fake topology response.",
                            Environment = "prod"
                        };
                        var result = FakeScenarioFactory.CreateTopologyResult(runId, taskId, dummyRequest);
                        return JsonSerializer.Serialize(result, jsonOptions);
                    }));
            }
        }
    }

    private static void RegisterRetrieval(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ITextChunker, SimpleTextChunker>();
        services.AddScoped<IRetrievalDocumentBuilder, RetrievalDocumentBuilder>();
        services.AddScoped<IRetrievalIndexingService, RetrievalIndexingService>();
        services.AddScoped<IRetrievalQueryService, RetrievalQueryService>();
        services.AddScoped<IRetrievalRunCompletionIndexer, RetrievalRunCompletionIndexer>();

        var vectorMode = configuration["Retrieval:VectorIndex"] ?? "InMemory";
        if (string.Equals(vectorMode, "AzureSearch", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IAzureSearchClient, NotConfiguredAzureSearchClient>();
            services.AddSingleton<IVectorIndex, AzureAiSearchVectorIndex>();
        }
        else
        {
            services.AddSingleton<IVectorIndex, InMemoryVectorIndex>();
        }

        var embedDeployment = configuration["AzureOpenAI:EmbeddingDeploymentName"];
        var endpoint = configuration["AzureOpenAI:Endpoint"];
        var apiKey = configuration["AzureOpenAI:ApiKey"];
        var useAzureEmbeddings = !string.IsNullOrWhiteSpace(embedDeployment)
            && !string.IsNullOrWhiteSpace(endpoint)
            && !string.IsNullOrWhiteSpace(apiKey);

        if (useAzureEmbeddings)
        {
            services.AddSingleton<IOpenAiEmbeddingClient>(_ =>
                new AzureOpenAiEmbeddingClient(endpoint!, apiKey!, embedDeployment!));
            services.AddSingleton<IEmbeddingService, AzureOpenAiEmbeddingService>();
        }
        else
        {
            services.AddSingleton<IEmbeddingService, FakeEmbeddingService>();
        }
    }
}
