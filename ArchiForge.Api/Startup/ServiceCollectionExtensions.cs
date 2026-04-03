using System.Text.Json;
using System.Text.Json.Serialization;

using ArchiForge.AgentRuntime;
using ArchiForge.AgentRuntime.Explanation;
using ArchiForge.AgentSimulator.Services;
using ArchiForge.Api.Ask;
using ArchiForge.Api.Configuration;
using ArchiForge.Api.Health;
using ArchiForge.Api.Hosting;
using ArchiForge.Api.Hosted;
using ArchiForge.Api.Jobs;

using ArchiForge.Application.Jobs;

using Azure.Core;
using Azure.Storage.Queues;

using ArchiForge.Persistence.BlobStore;
using ArchiForge.Api.Resilience;
using ArchiForge.Api.Services;
using ArchiForge.Api.Services.Ask;
using ArchiForge.Api.Services.Delivery;
using ArchiForge.Application;
using ArchiForge.Application.Agents;
using ArchiForge.Application.Analysis;
using ArchiForge.Application.Evolution;
using ArchiForge.Application.Bootstrap;
using ArchiForge.Application.Common;
using ArchiForge.Application.Determinism;
using ArchiForge.Application.Diagrams;
using ArchiForge.Application.Diffs;
using ArchiForge.Application.Evidence;
using ArchiForge.Application.Exports;
using ArchiForge.Application.Governance;
using ArchiForge.Application.Summaries;
using ArchiForge.ArtifactSynthesis.Docx;
using ArchiForge.ArtifactSynthesis.Generators;
using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Packaging;
using ArchiForge.ArtifactSynthesis.Renderers;
using ArchiForge.ArtifactSynthesis.Services;
using ArchiForge.ContextIngestion.Canonicalization;
using ArchiForge.ContextIngestion.Connectors;
using ArchiForge.ContextIngestion.Contracts;
using ArchiForge.ContextIngestion.Infrastructure;
using ArchiForge.ContextIngestion.Parsing;
using ArchiForge.ContextIngestion.Summaries;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Evolution;
using ArchiForge.Contracts.Requests;
using ArchiForge.Coordinator.Services;
using ArchiForge.Core.Ask;
using ArchiForge.Core.Resilience;
using ArchiForge.Data.Infrastructure;
using ArchiForge.Data.Repositories;
using ArchiForge.DecisionEngine.Services;
using ArchiForge.DecisionEngine.Validation;
using ArchiForge.Decisioning.Advisory.Analysis;
using ArchiForge.Decisioning.Advisory.Delivery;
using ArchiForge.Decisioning.Advisory.Learning;
using ArchiForge.Decisioning.Advisory.Scheduling;
using ArchiForge.Decisioning.Advisory.Services;
using ArchiForge.Decisioning.Alerts;
using ArchiForge.Decisioning.Alerts.Composite;
using ArchiForge.Decisioning.Alerts.Delivery;
using ArchiForge.Decisioning.Alerts.Simulation;
using ArchiForge.Decisioning.Alerts.Tuning;
using ArchiForge.Decisioning.Comparison;
using ArchiForge.Decisioning.Governance.PolicyPacks;
using ArchiForge.Decisioning.Governance.Resolution;
using ArchiForge.KnowledgeGraph.Inference;
using ArchiForge.KnowledgeGraph.Interfaces;
using ArchiForge.KnowledgeGraph.Mapping;
using ArchiForge.KnowledgeGraph.Services;
using ArchiForge.Persistence.Advisory;
using ArchiForge.Persistence.Alerts;
using ArchiForge.Persistence.Alerts.Simulation;
using ArchiForge.Persistence.Archival;
using ArchiForge.Persistence.Retrieval;
using ArchiForge.Retrieval.Chunking;
using ArchiForge.Retrieval.Embedding;
using ArchiForge.Retrieval.Indexing;
using ArchiForge.Retrieval.Queries;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using ContextConnector = ArchiForge.ContextIngestion.Interfaces.IContextConnector;
using ContextIngestionService = ArchiForge.ContextIngestion.Interfaces.IContextIngestionService;
using GraphBuilder = ArchiForge.KnowledgeGraph.Interfaces.IGraphBuilder;
using KnowledgeGraphService = ArchiForge.KnowledgeGraph.Interfaces.IKnowledgeGraphService;

namespace ArchiForge.Api.Startup;

internal static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddArchiForgeApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration,
        ArchiForgeHostingRole hostingRole)
    {
        services.Configure<DemoOptions>(configuration.GetSection(DemoOptions.SectionName));
        services.Configure<BatchReplayOptions>(configuration.GetSection(BatchReplayOptions.SectionName));
        services.Configure<ApiDeprecationOptions>(configuration.GetSection(ApiDeprecationOptions.SectionName));
        services.Configure<DataArchivalOptions>(configuration.GetSection(DataArchivalOptions.SectionName));
        services.AddScoped<IDemoSeedService, DemoSeedService>();
        services.AddArchiForgeStorage(configuration);
        RegisterAdvisoryScheduling(services, hostingRole);
        RegisterDigestDelivery(services, configuration);
        RegisterAlerts(services);
        RegisterDataInfrastructure(services, configuration);
        RegisterBackgroundJobs(services, configuration, hostingRole);
        RegisterRunExportAndArchitectureAnalysis(services, configuration);
        RegisterComparisonReplayAndDrift(services, configuration);
        RegisterRunReplayManifestAndDiffs(services);
        RegisterContextIngestionAndKnowledgeGraph(services);
        RegisterDecisioningEngines(services);
        RegisterCoordinatorDecisionEngineAndRepositories(services, configuration);
        RegisterArtifactSynthesis(services);
        RegisterAgentExecution(services, configuration);
        RegisterRetrieval(services, configuration);
        RegisterGovernance(services, configuration);
        RegisterRetrievalIndexingOutbox(services, hostingRole);
        RegisterDataArchivalHostedService(services, hostingRole);
        RegisterArchiForgeHealthChecks(services, hostingRole);
        return services;
    }

    private static void RegisterDataArchivalHostedService(IServiceCollection services, ArchiForgeHostingRole hostingRole)
    {
        if (hostingRole is not ArchiForgeHostingRole.Combined and not ArchiForgeHostingRole.Worker)
            return;

        services.AddSingleton<DataArchivalHostHealthState>();
        services.AddHostedService<DataArchivalHostedService>();
    }

    private static void RegisterRetrievalIndexingOutbox(IServiceCollection services, ArchiForgeHostingRole hostingRole)
    {
        services.AddSingleton<IRetrievalIndexingOutboxProcessor, RetrievalIndexingOutboxProcessor>();

        if (hostingRole is ArchiForgeHostingRole.Combined or ArchiForgeHostingRole.Worker)
            services.AddHostedService<RetrievalIndexingOutboxHostedService>();
    }

    private static void RegisterAdvisoryScheduling(IServiceCollection services, ArchiForgeHostingRole hostingRole)
    {
        services.AddScoped<IScanScheduleCalculator, SimpleScanScheduleCalculator>();
        services.AddScoped<IArchitectureDigestBuilder, ArchitectureDigestBuilder>();
        services.AddScoped<IAdvisoryScanRunner, AdvisoryScanRunner>();
        services.AddScoped<AdvisoryDueScheduleProcessor>();

        if (hostingRole is ArchiForgeHostingRole.Combined or ArchiForgeHostingRole.Worker)
            services.AddHostedService<AdvisoryScanHostedService>();
    }

    private static void RegisterDigestDelivery(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<WebhookDeliveryOptions>(configuration.GetSection(WebhookDeliveryOptions.SectionName));
        services.AddSingleton<IEmailSender, FakeEmailSender>();
        services
            .AddHttpClient(
                "ArchiForgeWebhooks",
                static client => client.Timeout = TimeSpan.FromSeconds(60));
        services.AddSingleton<HttpWebhookPoster>();
        services.AddSingleton<FakeWebhookPoster>();
        services.AddSingleton<IWebhookPoster>(static sp =>
        {
            IOptionsMonitor<WebhookDeliveryOptions> monitor = sp.GetRequiredService<IOptionsMonitor<WebhookDeliveryOptions>>();
            IWebhookPoster impl = monitor.CurrentValue.UseHttpClient
                ? sp.GetRequiredService<HttpWebhookPoster>()
                : sp.GetRequiredService<FakeWebhookPoster>();
            return new WebhookHmacEnvelopePoster(monitor, impl);
        });
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

        services.AddScoped<IAlertNoiseScorer, AlertNoiseScorer>();
        services.AddScoped<IThresholdRecommendationService, ThresholdRecommendationService>();

        services.AddScoped<IPolicyPackResolver, PolicyPackResolver>();
        services.AddScoped<IPolicyPackManagementService, PolicyPackManagementService>();
        services.AddScoped<IEffectiveGovernanceResolver, EffectiveGovernanceResolver>();
        services.AddScoped<EffectiveGovernanceLoader>();
        services.AddScoped<IEffectiveGovernanceLoader>(static sp =>
            new RequestScopedCachingEffectiveGovernanceLoader(sp.GetRequiredService<EffectiveGovernanceLoader>()));
        services.AddScoped<IPolicyPacksAppService, PolicyPacksAppService>();
    }

    private static void RegisterDataInfrastructure(IServiceCollection services, IConfiguration configuration)
    {
        ArchiForgeOptions mode = configuration
                                     .GetSection(ArchiForgeOptions.SectionName)
                                     .Get<ArchiForgeOptions>()
                                 ?? new ArchiForgeOptions();

        if (!string.Equals(mode.StorageProvider, "InMemory", StringComparison.OrdinalIgnoreCase))

            return;


        services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
    }

    private static void RegisterArchiForgeHealthChecks(IServiceCollection services, ArchiForgeHostingRole hostingRole)
    {
        IHealthChecksBuilder builder = services.AddHealthChecks()
            .AddCheck(
                "liveness",
                () => HealthCheckResult.Healthy("ArchiForge API process is running."),
                tags: [ReadinessTags.Live])
            .AddCheck<SqlConnectionHealthCheck>(
                "database",
                failureStatus: HealthStatus.Unhealthy,
                tags: [ReadinessTags.Ready])
            .AddCheck<SchemaFilesHealthCheck>("schema_files", tags: [ReadinessTags.Ready])
            .AddCheck<ComplianceRulePackHealthCheck>("compliance_rule_pack", tags: [ReadinessTags.Ready])
            .AddCheck<ProcessTempDirectoryHealthCheck>("temp_directory", tags: [ReadinessTags.Ready])
            .AddCheck<BlobStorageHealthCheck>("blob_storage", tags: [ReadinessTags.Ready]);

        if (hostingRole is ArchiForgeHostingRole.Combined or ArchiForgeHostingRole.Worker)
        {
            builder.AddCheck<DataArchivalHostHealthCheck>(
                "data_archival",
                failureStatus: HealthStatus.Degraded,
                tags: [ReadinessTags.Ready]);
        }
    }

    private static void RegisterBackgroundJobs(
        IServiceCollection services,
        IConfiguration configuration,
        ArchiForgeHostingRole hostingRole)
    {
        services.Configure<BackgroundJobsOptions>(configuration.GetSection(BackgroundJobsOptions.SectionName));

        BackgroundJobsOptions jobsSnapshot =
            configuration.GetSection(BackgroundJobsOptions.SectionName).Get<BackgroundJobsOptions>() ??
            new BackgroundJobsOptions();

        bool durable = string.Equals(jobsSnapshot.Mode, "Durable", StringComparison.OrdinalIgnoreCase);

        services.AddScoped<IBackgroundJobWorkUnitExecutor, BackgroundJobWorkUnitExecutor>();

        if (hostingRole == ArchiForgeHostingRole.Worker)
        {
            if (durable)
            {
                RegisterDurableBackgroundJobInfrastructure(services);
                services.AddHostedService<BackgroundJobQueueProcessorHostedService>();
            }

            return;
        }

        if (hostingRole is not (ArchiForgeHostingRole.Api or ArchiForgeHostingRole.Combined))
            return;

        if (durable)
        {
            RegisterDurableBackgroundJobInfrastructure(services);
            services.AddSingleton<IBackgroundJobQueueNotifySender, AzureStorageQueueBackgroundJobNotifySender>();
            services.AddSingleton<IBackgroundJobQueue, DurableBackgroundJobQueue>();
        }
        else
        {
            services.AddSingleton<IBackgroundJobQueue, InMemoryBackgroundJobQueue>();

            services.AddHostedService(static sp => (InMemoryBackgroundJobQueue)sp.GetRequiredService<IBackgroundJobQueue>());
        }
    }

    private static void RegisterDurableBackgroundJobInfrastructure(IServiceCollection services)
    {
        services.AddScoped<IBackgroundJobRepository, BackgroundJobRepository>();
        services.AddSingleton<IBackgroundJobResultBlobAccessor, AzureBlobBackgroundJobResultBlobAccessor>();
        services.AddSingleton(static sp => CreateBackgroundJobsQueueClient(sp));
    }

    private static QueueClient CreateBackgroundJobsQueueClient(IServiceProvider serviceProvider)
    {
        BackgroundJobsOptions jobsOptions =
            serviceProvider.GetRequiredService<IOptions<BackgroundJobsOptions>>().Value;

        ArtifactLargePayloadOptions? largePayload =
            serviceProvider.GetService<IOptions<ArtifactLargePayloadOptions>>()?.Value;

        TokenCredential credential = serviceProvider.GetRequiredService<TokenCredential>();
        Uri? queueUri = BackgroundJobQueueAddress.ResolveQueueServiceUri(jobsOptions, largePayload);

        if (queueUri is null)
            throw new InvalidOperationException(
                "BackgroundJobs:QueueServiceUri is missing and could not be derived from ArtifactLargePayload:AzureBlobServiceUri. Configure a queue endpoint for durable jobs.");

        if (string.IsNullOrWhiteSpace(jobsOptions.QueueName))
            throw new InvalidOperationException("BackgroundJobs:QueueName is required when BackgroundJobs:Mode is Durable.");

        QueueServiceClient serviceClient = new(queueUri, credential);

        return serviceClient.GetQueueClient(jobsOptions.QueueName);
    }

    private static void RegisterRunExportAndArchitectureAnalysis(IServiceCollection services, IConfiguration configuration)
    {
        ArchiForgeOptions exportStorage = configuration
                                              .GetSection(ArchiForgeOptions.SectionName)
                                              .Get<ArchiForgeOptions>()
                                          ?? new ArchiForgeOptions();

        if (string.Equals(exportStorage.StorageProvider, "InMemory", StringComparison.OrdinalIgnoreCase))

            services.AddSingleton<IRunExportRecordRepository, InMemoryRunExportRecordRepository>();

        else

            services.AddScoped<IRunExportRecordRepository, RunExportRecordRepository>();

        services.AddScoped<IRunExportAuditService, RunExportAuditService>();
        services.AddScoped<IArchitectureApplicationService, ArchitectureApplicationService>();
        services.AddScoped<IArchitectureAnalysisService, ArchitectureAnalysisService>();
        services.AddScoped<ISimulationEngine, SimulationEngine>();
        services.AddScoped<IShadowExecutionService, ShadowExecutionService>();
        services.AddScoped<ISimulationEvaluationService, SimulationEvaluationService>();
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

    private static void RegisterComparisonReplayAndDrift(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ReplayDiagnosticsOptions>(configuration.GetSection(ReplayDiagnosticsOptions.SectionName));

        ArchiForgeOptions storageMode = configuration
                                            .GetSection(ArchiForgeOptions.SectionName)
                                            .Get<ArchiForgeOptions>()
                                        ?? new ArchiForgeOptions();

        if (string.Equals(storageMode.StorageProvider, "InMemory", StringComparison.OrdinalIgnoreCase))

            services.AddSingleton<IComparisonRecordRepository, InMemoryComparisonRecordRepository>();

        else

            services.AddScoped<IComparisonRecordRepository, ComparisonRecordRepository>();

        services.AddScoped<IComparisonAuditService, ComparisonAuditService>();
        services.AddScoped<IComparisonDriftAnalyzer, ComparisonDriftAnalyzer>();
        services.AddScoped<IComparisonReplayService, ComparisonReplayService>();
        services.AddScoped<IComparisonReplayCostEstimator, ComparisonReplayCostEstimator>();
        services.AddScoped<IComparisonReplayApiService, ComparisonReplayApiService>();
        services.AddScoped<IComparisonDriftReportExportService, ComparisonDriftReportExportService>();
        services.AddSingleton<IReplayDiagnosticsRecorder, ReplayDiagnosticsRecorder>();
    }

    private static void RegisterRunReplayManifestAndDiffs(IServiceCollection services)
    {
        services.AddScoped<IActorContext, ActorContext>();
        services.AddScoped<IBaselineMutationAuditService, BaselineMutationAuditService>();
        services.AddScoped<IArchitectureRunService, ArchitectureRunService>();
        services.AddScoped<IRunDetailQueryService, RunDetailQueryService>();
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
        services.AddSingleton<PlainTextContextDocumentParser>();
        services.AddSingleton<IContextDocumentParser>(static sp => sp.GetRequiredService<PlainTextContextDocumentParser>());
        services.AddSingleton<IReadOnlyList<IContextDocumentParser>>(static sp =>
            ContextDocumentParserPipeline.CreateOrderedContextDocumentParsers(sp));

        services.AddSingleton<IInfrastructureDeclarationParser, JsonInfrastructureDeclarationParser>();
        services.AddSingleton<IInfrastructureDeclarationParser, SimpleTerraformDeclarationParser>();

        // Concrete connectors (registered once each). Order here matches pipeline order for readability only;
        // execution order is defined solely in ContextConnectorPipeline.CreateOrderedContextConnectorPipeline.
        services.AddSingleton<StaticRequestContextConnector>();
        services.AddSingleton<InlineRequirementsConnector>();
        services.AddSingleton<DocumentConnector>();
        services.AddSingleton<PolicyReferenceConnector>();
        services.AddSingleton<TopologyHintsConnector>();
        services.AddSingleton<SecurityBaselineHintsConnector>();
        services.AddSingleton<InfrastructureDeclarationConnector>();

        // IEnumerable<IContextConnector> must come only from CreateOrderedContextConnectorPipeline — preserves
        // deterministic DeltaSummary segment order and operator-facing narrative (see docs/CONTEXT_INGESTION.md).
        services.AddSingleton<IEnumerable<ContextConnector>>(static sp =>
            ContextConnectorPipeline.CreateOrderedContextConnectorPipeline(sp));

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
        services.AddScoped<IEvidenceBuilder, DefaultEvidenceBuilder>();
        services.AddScoped<IAgentExecutionTraceRecorder, AgentExecutionTraceRecorder>();

        ArchiForgeOptions coordinatorStorage = configuration
                                                   .GetSection(ArchiForgeOptions.SectionName)
                                                   .Get<ArchiForgeOptions>()
                                               ?? new ArchiForgeOptions();

        if (string.Equals(coordinatorStorage.StorageProvider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IArchitectureRequestRepository, InMemoryArchitectureRequestRepository>();
            services.AddSingleton<IArchitectureRunRepository>(sp =>
                new InMemoryArchitectureRunRepository(sp.GetRequiredService<IArchitectureRequestRepository>()));
            services.AddSingleton<IArchitectureRunIdempotencyRepository, InMemoryArchitectureRunIdempotencyRepository>();
            services.AddSingleton<IAgentTaskRepository, InMemoryAgentTaskRepository>();
            services.AddSingleton<IAgentResultRepository, InMemoryAgentResultRepository>();
            services.AddSingleton<IAgentEvaluationRepository, InMemoryAgentEvaluationRepository>();
            services.AddSingleton<IDecisionNodeRepository, InMemoryDecisionNodeRepository>();
            services.AddSingleton<IGoldenManifestRepository, InMemoryCoordinatorGoldenManifestRepository>();
            services.AddSingleton<IEvidenceBundleRepository, InMemoryEvidenceBundleRepository>();
            services.AddSingleton<IDecisionTraceRepository, InMemoryCoordinatorDecisionTraceRepository>();
            services.AddSingleton<IAgentEvidencePackageRepository, InMemoryAgentEvidencePackageRepository>();
            services.AddSingleton<IAgentExecutionTraceRepository, InMemoryAgentExecutionTraceRepository>();
        }
        else
        {
            services.AddScoped<IAgentEvaluationRepository, AgentEvaluationRepository>();
            services.AddScoped<IDecisionNodeRepository, DecisionNodeRepository>();
            services.AddScoped<IArchitectureRequestRepository, ArchitectureRequestRepository>();
            services.AddScoped<IArchitectureRunRepository, ArchitectureRunRepository>();
            services.AddScoped<IArchitectureRunIdempotencyRepository, ArchitectureRunIdempotencyRepository>();
            services.AddScoped<IAgentTaskRepository, AgentTaskRepository>();
            services.AddScoped<IAgentResultRepository, AgentResultRepository>();
            // Data-layer contracts (CreateAsync / GetByVersionAsync / batch traces) — distinct from
            // Decisioning.Interfaces.IGoldenManifestRepository / IDecisionTraceRepository registered in AddArchiForgeStorage.
            services.AddScoped<IGoldenManifestRepository, GoldenManifestRepository>();
            services.AddScoped<IEvidenceBundleRepository, EvidenceBundleRepository>();
            services.AddScoped<IDecisionTraceRepository, DecisionTraceRepository>();
            services.AddScoped<IAgentEvidencePackageRepository, AgentEvidencePackageRepository>();
            services.AddScoped<IAgentExecutionTraceRepository, AgentExecutionTraceRepository>();
        }
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
        string? agentMode = configuration["AgentExecution:Mode"];
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

            string? azureOpenAiEndpoint = configuration["AzureOpenAI:Endpoint"];
            string? azureOpenAiKey = configuration["AzureOpenAI:ApiKey"];
            string? azureOpenAiDeployment = configuration["AzureOpenAI:DeploymentName"];
            bool useAzureOpenAi = !string.IsNullOrWhiteSpace(azureOpenAiEndpoint)
                                  && !string.IsNullOrWhiteSpace(azureOpenAiKey)
                                  && !string.IsNullOrWhiteSpace(azureOpenAiDeployment);

            if (useAzureOpenAi)
            {
                services.AddKeyedSingleton<CircuitBreakerGate>(
                    OpenAiCircuitBreakerKeys.Completion,
                    (sp, _) => new CircuitBreakerGate(ResolveOpenAiCircuitBreakerOptions(sp.GetRequiredService<IConfiguration>())));

                services.AddSingleton<IAgentCompletionClient>(sp =>
                {
                    IConfiguration config = sp.GetRequiredService<IConfiguration>();
                    string endpoint = config["AzureOpenAI:Endpoint"]
                                      ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is missing.");
                    string apiKey = config["AzureOpenAI:ApiKey"]
                                    ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is missing.");
                    string deploymentName = config["AzureOpenAI:DeploymentName"]
                                            ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName is missing.");
                    AzureOpenAiCompletionClient inner = new(endpoint, apiKey, deploymentName);
                    CircuitBreakerGate gate = sp.GetRequiredKeyedService<CircuitBreakerGate>(OpenAiCircuitBreakerKeys.Completion);
                    ILogger<CircuitBreakingAgentCompletionClient> logger =
                        sp.GetRequiredService<ILogger<CircuitBreakingAgentCompletionClient>>();
                    return new CircuitBreakingAgentCompletionClient(inner, gate, logger);
                });
            }
            else

                RegisterFakeAgentCompletionClient(services);

        }
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
                        runId = span.Length > 6 ? span[6..].Trim().ToString() : runId;
                    else if (span.StartsWith("TaskId:", StringComparison.OrdinalIgnoreCase))
                        taskId = span.Length > 7 ? span[7..].Trim().ToString() : taskId;
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
        ArchiForgeOptions governanceStorage = configuration
                                                  .GetSection(ArchiForgeOptions.SectionName)
                                                  .Get<ArchiForgeOptions>()
                                              ?? new ArchiForgeOptions();

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

            services.AddSingleton<IVectorIndex, InMemoryVectorIndex>();


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

            services.AddSingleton<IEmbeddingService, FakeEmbeddingService>();

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
