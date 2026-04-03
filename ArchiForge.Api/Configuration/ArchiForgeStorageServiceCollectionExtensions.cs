using System.Reflection;

using ArchiForge.Api.DataAccess;
using ArchiForge.Api.Services.Evolution;
using ArchiForge.Api.Services.Learning;
using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Repositories;
using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.ContextIngestion.Repositories;
using ArchiForge.Contracts.Evolution;
using ArchiForge.Contracts.ProductLearning;
using ArchiForge.Contracts.ProductLearning.Planning;
using ArchiForge.Decisioning.Advisory.Delivery;
using ArchiForge.Decisioning.Advisory.Learning;
using ArchiForge.Decisioning.Advisory.Workflow;
using ArchiForge.Decisioning.Alerts;
using ArchiForge.Decisioning.Alerts.Composite;
using ArchiForge.Decisioning.Alerts.Delivery;
using ArchiForge.Decisioning.Governance.PolicyPacks;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Repositories;
using ArchiForge.KnowledgeGraph.Interfaces;
using ArchiForge.KnowledgeGraph.Repositories;
using ArchiForge.Persistence.Advisory;
using ArchiForge.Persistence.Alerts;
using ArchiForge.Persistence.Archival;
using ArchiForge.Persistence.Audit;
using ArchiForge.Persistence.BlobStore;
using ArchiForge.Persistence.Caching;
using ArchiForge.Persistence.Compare;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.Conversation;
using ArchiForge.Persistence.Evolution;
using ArchiForge.Persistence.Governance;
using ArchiForge.Persistence.Interfaces;
using ArchiForge.Persistence.Orchestration;
using ArchiForge.Persistence.ProductLearning;
using ArchiForge.Persistence.ProductLearning.Planning;
using ArchiForge.Persistence.Provenance;
using ArchiForge.Persistence.Queries;
using ArchiForge.Persistence.Replay;
using ArchiForge.Persistence.Repositories;
using ArchiForge.Persistence.Retrieval;
using ArchiForge.Persistence.Sql;
using ArchiForge.Persistence.Transactions;
using ArchiForge.Provenance;

using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ArchiForge.Api.Configuration;

public static class ArchiForgeStorageServiceCollectionExtensions
{
    public static IServiceCollection AddArchiForgeStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArchiForgeOptions options = configuration
                                        .GetSection(ArchiForgeOptions.SectionName)
                                        .Get<ArchiForgeOptions>()
                                    ?? new ArchiForgeOptions();

        services.Configure<ArchiForgeOptions>(
            configuration.GetSection(ArchiForgeOptions.SectionName));

        if (string.Equals(options.StorageProvider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IContextSnapshotRepository, InMemoryContextSnapshotRepository>();
            services.AddSingleton<IGraphSnapshotRepository, InMemoryGraphSnapshotRepository>();
            services.AddSingleton<IFindingsSnapshotRepository, InMemoryFindingsSnapshotRepository>();
            services.AddSingleton<IDecisionTraceRepository, InMemoryDecisionTraceRepository>();
            services.AddSingleton<IGoldenManifestRepository, InMemoryGoldenManifestRepository>();
            services.AddSingleton<IArtifactBundleRepository, InMemoryArtifactBundleRepository>();
            services.AddSingleton<IRunRepository, InMemoryRunRepository>();
            services.AddSingleton<IAuthorityQueryService, InMemoryAuthorityQueryService>();
            services.AddSingleton<IArtifactQueryService, InMemoryArtifactQueryService>();
            services.AddScoped<IAuthorityCompareService, AuthorityCompareService>();
            services.AddScoped<IAuthorityReplayService, AuthorityReplayService>();
            services.AddSingleton<IAuditRepository, InMemoryAuditRepository>();
            services.AddSingleton<IProvenanceSnapshotRepository, InMemoryProvenanceSnapshotRepository>();
            services.AddScoped<IProvenanceQueryService, ProvenanceQueryService>();
            services.AddSingleton<IRecommendationRepository, InMemoryRecommendationRepository>();
            services.AddScoped<IRecommendationWorkflowService, RecommendationWorkflowService>();
            services.AddScoped<IRecommendationFeedbackAnalyzer, RecommendationFeedbackAnalyzer>();
            services.AddSingleton<IRecommendationLearningProfileRepository, InMemoryRecommendationLearningProfileRepository>();
            services.AddScoped<IRecommendationLearningService, RecommendationLearningService>();
            services.AddSingleton<IAdvisoryScanScheduleRepository, InMemoryAdvisoryScanScheduleRepository>();
            services.AddSingleton<IAdvisoryScanExecutionRepository, InMemoryAdvisoryScanExecutionRepository>();
            services.AddSingleton<IArchitectureDigestRepository, InMemoryArchitectureDigestRepository>();
            services.AddSingleton<IDigestSubscriptionRepository, InMemoryDigestSubscriptionRepository>();
            services.AddSingleton<IDigestDeliveryAttemptRepository, InMemoryDigestDeliveryAttemptRepository>();
            services.AddSingleton<IAlertRuleRepository, InMemoryAlertRuleRepository>();
            services.AddSingleton<IAlertRecordRepository, InMemoryAlertRecordRepository>();
            services.AddSingleton<IAlertRoutingSubscriptionRepository, InMemoryAlertRoutingSubscriptionRepository>();
            services.AddSingleton<IAlertDeliveryAttemptRepository, InMemoryAlertDeliveryAttemptRepository>();
            services.AddSingleton<ICompositeAlertRuleRepository, InMemoryCompositeAlertRuleRepository>();
            services.AddSingleton<IPolicyPackRepository, InMemoryPolicyPackRepository>();
            services.AddSingleton<IPolicyPackVersionRepository, InMemoryPolicyPackVersionRepository>();
            services.AddSingleton<IPolicyPackAssignmentRepository, InMemoryPolicyPackAssignmentRepository>();
            // Required by CoordinatorService → IAuthorityRunOrchestrator.
            services.AddSingleton<IArchiForgeUnitOfWorkFactory, InMemoryArchiForgeUnitOfWorkFactory>();
            services.AddSingleton<IRetrievalIndexingOutboxRepository, InMemoryRetrievalIndexingOutboxRepository>();
            services.AddSingleton<IProductLearningPilotSignalRepository, InMemoryProductLearningPilotSignalRepository>();
            services.AddSingleton<IProductLearningPlanningRepository, InMemoryProductLearningPlanningRepository>();
            services.AddSingleton<IImprovementThemeExtractionService, ImprovementThemeExtractionService>();
            services.AddSingleton<IImprovementPlanningService, ImprovementPlanningService>();
            services.AddSingleton<ICandidateChangeSetService, CandidateChangeSetService>();
            services.AddSingleton<IImprovementPlanPrioritizationService, ImprovementPlanPrioritizationService>();
            services.AddSingleton<IProductLearningFeedbackAggregationService, ProductLearningFeedbackAggregationService>();
            services.AddSingleton<IProductLearningImprovementOpportunityService, ProductLearningImprovementOpportunityService>();
            services.AddSingleton<IProductLearningDashboardService, ProductLearningDashboardService>();
            services.AddSingleton<Api.Services.Learning.ILearningPlanningReadService, Api.Services.Learning.LearningPlanningReadService>();
            services.AddSingleton<IEvolutionCandidateChangeSetRepository, InMemoryEvolutionCandidateChangeSetRepository>();
            services.AddSingleton<IEvolutionSimulationRunRepository, InMemoryEvolutionSimulationRunRepository>();
            services.AddScoped<IEvolutionSimulationService, EvolutionSimulationService>();
            services.AddSingleton<IConversationThreadRepository, InMemoryConversationThreadRepository>();
            services.AddSingleton<IConversationMessageRepository, InMemoryConversationMessageRepository>();
            services.AddScoped<IAuthorityRunOrchestrator, AuthorityRunOrchestrator>();
            services.AddScoped<IDataArchivalCoordinator, DataArchivalCoordinator>();
            return services;
        }

        // Singleton: one policy governs all read-time JSON fallback decisions.
        // Change mode to WarnOnJsonFallback during migration roll-out or RequireRelational after backfill.
        services.AddSingleton(sp => new Persistence.RelationalRead.JsonFallbackPolicy(
            Persistence.RelationalRead.PersistenceReadMode.AllowJsonFallback,
            sp.GetRequiredService<ILoggerFactory>().CreateLogger("ArchiForge.Persistence.JsonFallback")));

        string connectionString = configuration.GetConnectionString("ArchiForge")
                                  ?? throw new InvalidOperationException("Missing connection string 'ArchiForge'.");

        services.Configure<SqlRowLevelSecurityOptions>(configuration.GetSection(SqlRowLevelSecurityOptions.SectionName));
        services.Configure<ReadReplicaOptions>(configuration.GetSection(ReadReplicaOptions.SectionName));

        RegisterArtifactLargePayloadBlobStore(services, configuration);

        RegisterHotPathReadCaching(services, configuration);

        services.AddSingleton<SqlConnectionFactory>(
            _ => new SqlConnectionFactory(connectionString));
        services.AddSingleton<ResilientSqlConnectionFactory>(sp =>
            new ResilientSqlConnectionFactory(
                sp.GetRequiredService<SqlConnectionFactory>(),
                sp.GetRequiredService<ILogger<ResilientSqlConnectionFactory>>()));

        services.AddScoped<IRlsSessionContextApplicator, RlsSessionContextApplicator>();
        services.AddScoped<ISqlConnectionFactory>(sp =>
        {
            SqlRowLevelSecurityOptions rls =
                sp.GetRequiredService<IOptionsMonitor<SqlRowLevelSecurityOptions>>().CurrentValue;
            ResilientSqlConnectionFactory resilient = sp.GetRequiredService<ResilientSqlConnectionFactory>();

            if (!rls.ApplySessionContext)
                return resilient;

            return new SessionContextSqlConnectionFactory(
                resilient,
                sp.GetRequiredService<IRlsSessionContextApplicator>(),
                sp.GetRequiredService<ILogger<SessionContextSqlConnectionFactory>>());
        });

        services.AddScoped<IAuthorityRunListConnectionFactory, AuthorityRunListConnectionFactory>();

        Assembly persistenceAssembly = typeof(SqlSchemaBootstrapper).Assembly;
        string dir = Path.GetDirectoryName(persistenceAssembly.Location) ?? AppContext.BaseDirectory;
        string scriptPath = Path.Combine(dir, "Scripts", "ArchiForge.sql");

        services.AddScoped<ISchemaBootstrapper>(sp =>
            new SqlSchemaBootstrapper(
                sp.GetRequiredService<ISqlConnectionFactory>(),
                scriptPath));

        services.AddScoped<IContextSnapshotRepository, SqlContextSnapshotRepository>();
        services.AddScoped<IGraphSnapshotRepository, SqlGraphSnapshotRepository>();
        services.AddScoped<IFindingsSnapshotRepository, SqlFindingsSnapshotRepository>();
        services.AddScoped<IDecisionTraceRepository, SqlDecisionTraceRepository>();
        RegisterGoldenManifestRunAndPolicyPackRepositories(services, configuration);

        services.AddScoped<IArtifactBundleRepository, SqlArtifactBundleRepository>();
        services.AddScoped<IAuthorityQueryService, DapperAuthorityQueryService>();
        services.AddScoped<IArtifactQueryService, DapperArtifactQueryService>();
        services.AddScoped<IAuthorityCompareService, AuthorityCompareService>();
        services.AddScoped<IAuthorityReplayService, AuthorityReplayService>();
        services.AddScoped<IArchiForgeUnitOfWorkFactory, DapperArchiForgeUnitOfWorkFactory>();
        services.AddScoped<IRetrievalIndexingOutboxRepository, DapperRetrievalIndexingOutboxRepository>();
        services.AddScoped<IProductLearningPilotSignalRepository, DapperProductLearningPilotSignalRepository>();
        services.AddScoped<IProductLearningPlanningRepository, DapperProductLearningPlanningRepository>();
        services.AddScoped<IImprovementThemeExtractionService, ImprovementThemeExtractionService>();
        services.AddScoped<IImprovementPlanningService, ImprovementPlanningService>();
        services.AddScoped<ICandidateChangeSetService, CandidateChangeSetService>();
        services.AddScoped<IImprovementPlanPrioritizationService, ImprovementPlanPrioritizationService>();
        services.AddScoped<IProductLearningFeedbackAggregationService, ProductLearningFeedbackAggregationService>();
        services.AddScoped<IProductLearningImprovementOpportunityService, ProductLearningImprovementOpportunityService>();
        services.AddScoped<IProductLearningDashboardService, ProductLearningDashboardService>();
        services.AddScoped<ILearningPlanningReadService, LearningPlanningReadService>();
        services.AddScoped<IEvolutionCandidateChangeSetRepository, DapperEvolutionCandidateChangeSetRepository>();
        services.AddScoped<IEvolutionSimulationRunRepository, DapperEvolutionSimulationRunRepository>();
        services.AddScoped<IEvolutionSimulationService, EvolutionSimulationService>();
        services.AddScoped<IAuthorityRunOrchestrator, AuthorityRunOrchestrator>();
        services.AddScoped<IAuditRepository, DapperAuditRepository>();
        services.AddScoped<IProvenanceSnapshotRepository, SqlProvenanceSnapshotRepository>();
        services.AddScoped<IProvenanceQueryService, ProvenanceQueryService>();
        services.AddScoped<IConversationThreadRepository, DapperConversationThreadRepository>();
        services.AddScoped<IConversationMessageRepository, DapperConversationMessageRepository>();
        services.AddScoped<IRecommendationRepository, DapperRecommendationRepository>();
        services.AddScoped<IRecommendationWorkflowService, RecommendationWorkflowService>();
        services.AddScoped<IRecommendationFeedbackAnalyzer, RecommendationFeedbackAnalyzer>();
        services.AddScoped<IRecommendationLearningProfileRepository, DapperRecommendationLearningProfileRepository>();
        services.AddScoped<IRecommendationLearningService, RecommendationLearningService>();
        services.AddScoped<IAdvisoryScanScheduleRepository, DapperAdvisoryScanScheduleRepository>();
        services.AddScoped<IAdvisoryScanExecutionRepository, DapperAdvisoryScanExecutionRepository>();
        services.AddScoped<IArchitectureDigestRepository, DapperArchitectureDigestRepository>();
        services.AddScoped<IDigestSubscriptionRepository, DapperDigestSubscriptionRepository>();
        services.AddScoped<IDigestDeliveryAttemptRepository, DapperDigestDeliveryAttemptRepository>();
        services.AddScoped<IAlertRuleRepository, DapperAlertRuleRepository>();
        services.AddScoped<IAlertRecordRepository, DapperAlertRecordRepository>();
        services.AddScoped<IAlertRoutingSubscriptionRepository, DapperAlertRoutingSubscriptionRepository>();
        services.AddScoped<IAlertDeliveryAttemptRepository, DapperAlertDeliveryAttemptRepository>();
        services.AddScoped<ICompositeAlertRuleRepository, DapperCompositeAlertRuleRepository>();
        services.AddScoped<IPolicyPackVersionRepository, DapperPolicyPackVersionRepository>();
        services.AddScoped<IPolicyPackAssignmentRepository, DapperPolicyPackAssignmentRepository>();
        services.AddScoped<IDataArchivalCoordinator, DataArchivalCoordinator>();

        services.AddSingleton<Data.Infrastructure.IDbConnectionFactory>(p =>
            new SqlScopedResolutionDbConnectionFactory(
                p.GetRequiredService<IServiceScopeFactory>(),
                connectionString));

        return services;
    }

    private static void RegisterHotPathReadCaching(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<HotPathCacheOptions>(
            configuration.GetSection(HotPathCacheOptions.SectionName));

        HotPathCacheOptions snapshot = configuration
                                           .GetSection(HotPathCacheOptions.SectionName)
                                           .Get<HotPathCacheOptions>()
                                       ?? new HotPathCacheOptions();

        if (!snapshot.Enabled)
            return;

        string provider = snapshot.Provider ?? "Memory";

        if (string.Equals(provider, "Redis", StringComparison.OrdinalIgnoreCase))
        {
            string redis = snapshot.RedisConnectionString?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(redis))
            {
                throw new InvalidOperationException(
                    "HotPathCache:RedisConnectionString is required when HotPathCache:Provider is Redis.");
            }

            services.AddStackExchangeRedisCache(o => o.Configuration = redis);
            services.AddSingleton<IHotPathReadCache, DistributedHotPathReadCache>();
            return;
        }

        services.AddMemoryCache();
        services.AddSingleton<IHotPathReadCache, MemoryHotPathReadCache>();
    }

    private static void RegisterGoldenManifestRunAndPolicyPackRepositories(
        IServiceCollection services,
        IConfiguration configuration)
    {
        HotPathCacheOptions hotPath = configuration
                                          .GetSection(HotPathCacheOptions.SectionName)
                                          .Get<HotPathCacheOptions>()
                                      ?? new HotPathCacheOptions();

        if (!hotPath.Enabled)
        {
            services.AddScoped<IGoldenManifestRepository, SqlGoldenManifestRepository>();
            services.AddScoped<IRunRepository, SqlRunRepository>();
            services.AddScoped<IPolicyPackRepository, DapperPolicyPackRepository>();
            return;
        }

        services.AddScoped<SqlGoldenManifestRepository>();
        services.AddScoped<IGoldenManifestRepository>(sp => new CachingGoldenManifestRepository(
            sp.GetRequiredService<SqlGoldenManifestRepository>(),
            sp.GetRequiredService<IHotPathReadCache>()));

        services.AddScoped<SqlRunRepository>();
        services.AddScoped<IRunRepository>(sp => new CachingRunRepository(
            sp.GetRequiredService<SqlRunRepository>(),
            sp.GetRequiredService<IHotPathReadCache>()));

        services.AddScoped<DapperPolicyPackRepository>();
        services.AddScoped<IPolicyPackRepository>(sp => new CachingPolicyPackRepository(
            sp.GetRequiredService<DapperPolicyPackRepository>(),
            sp.GetRequiredService<IHotPathReadCache>()));
    }

    private static void RegisterArtifactLargePayloadBlobStore(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ArtifactLargePayloadOptions>(
            configuration.GetSection(ArtifactLargePayloadOptions.SectionName));

        ArtifactLargePayloadOptions snapshot = configuration
                                                   .GetSection(ArtifactLargePayloadOptions.SectionName)
                                                   .Get<ArtifactLargePayloadOptions>()
                                               ?? new ArtifactLargePayloadOptions();

        string provider = snapshot.BlobProvider ?? "None";

        if (string.Equals(provider, "AzureBlob", StringComparison.OrdinalIgnoreCase))
        {
            string uriText = snapshot.AzureBlobServiceUri ?? string.Empty;

            if (string.IsNullOrWhiteSpace(uriText))
            {
                throw new InvalidOperationException(
                    "ArtifactLargePayload:AzureBlobServiceUri is required when BlobProvider is AzureBlob.");
            }

            Uri serviceUri = new(uriText, UriKind.Absolute);
            services.AddSingleton<TokenCredential>(_ => new DefaultAzureCredential());
            services.AddSingleton(sp =>
                new BlobServiceClient(serviceUri, sp.GetRequiredService<TokenCredential>()));
            services.AddSingleton<IArtifactBlobStore>(sp =>
                new AzureBlobArtifactBlobStore(
                    sp.GetRequiredService<BlobServiceClient>(),
                    sp.GetRequiredService<TokenCredential>()));
        }
        else if (string.Equals(provider, "Local", StringComparison.OrdinalIgnoreCase))
        {
            string root = string.IsNullOrWhiteSpace(snapshot.LocalRootPath)
                ? Path.Combine(AppContext.BaseDirectory, "blob-store")
                : snapshot.LocalRootPath;
            services.AddSingleton<IArtifactBlobStore>(_ => new LocalFileArtifactBlobStore(root));
        }
        else
        {
            services.AddSingleton<IArtifactBlobStore, NullArtifactBlobStore>();
        }
    }
}
