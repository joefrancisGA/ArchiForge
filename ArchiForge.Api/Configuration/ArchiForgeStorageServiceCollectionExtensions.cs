using System.Reflection;

using ArchiForge.Api.DataAccess;
using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Repositories;
using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.ContextIngestion.Repositories;
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
using ArchiForge.Persistence.Audit;
using ArchiForge.Persistence.Compare;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.Conversation;
using ArchiForge.Persistence.Governance;
using ArchiForge.Persistence.Interfaces;
using ArchiForge.Persistence.Orchestration;
using ArchiForge.Persistence.Archival;
using ArchiForge.Contracts.ProductLearning;
using ArchiForge.Contracts.ProductLearning.Planning;
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
            services.AddSingleton<IProductLearningFeedbackAggregationService, ProductLearningFeedbackAggregationService>();
            services.AddSingleton<IProductLearningImprovementOpportunityService, ProductLearningImprovementOpportunityService>();
            services.AddSingleton<IProductLearningDashboardService, ProductLearningDashboardService>();
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

        services.AddSingleton<ArchiForge.Persistence.Connections.SqlConnectionFactory>(
            _ => new ArchiForge.Persistence.Connections.SqlConnectionFactory(connectionString));
        services.AddSingleton<ResilientSqlConnectionFactory>(sp =>
            new ResilientSqlConnectionFactory(
                sp.GetRequiredService<ArchiForge.Persistence.Connections.SqlConnectionFactory>(),
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
        services.AddScoped<IGoldenManifestRepository, SqlGoldenManifestRepository>();
        services.AddScoped<IArtifactBundleRepository, SqlArtifactBundleRepository>();
        services.AddScoped<IRunRepository, SqlRunRepository>();
        services.AddScoped<IAuthorityQueryService, DapperAuthorityQueryService>();
        services.AddScoped<IArtifactQueryService, DapperArtifactQueryService>();
        services.AddScoped<IAuthorityCompareService, AuthorityCompareService>();
        services.AddScoped<IAuthorityReplayService, AuthorityReplayService>();
        services.AddScoped<IArchiForgeUnitOfWorkFactory, DapperArchiForgeUnitOfWorkFactory>();
        services.AddScoped<IRetrievalIndexingOutboxRepository, DapperRetrievalIndexingOutboxRepository>();
        services.AddScoped<IProductLearningPilotSignalRepository, DapperProductLearningPilotSignalRepository>();
        services.AddScoped<IProductLearningPlanningRepository, DapperProductLearningPlanningRepository>();
        services.AddScoped<IImprovementThemeExtractionService, ImprovementThemeExtractionService>();
        services.AddScoped<IProductLearningFeedbackAggregationService, ProductLearningFeedbackAggregationService>();
        services.AddScoped<IProductLearningImprovementOpportunityService, ProductLearningImprovementOpportunityService>();
        services.AddScoped<IProductLearningDashboardService, ProductLearningDashboardService>();
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
        services.AddScoped<IPolicyPackRepository, DapperPolicyPackRepository>();
        services.AddScoped<IPolicyPackVersionRepository, DapperPolicyPackVersionRepository>();
        services.AddScoped<IPolicyPackAssignmentRepository, DapperPolicyPackAssignmentRepository>();
        services.AddScoped<IDataArchivalCoordinator, DataArchivalCoordinator>();

        services.AddSingleton<ArchiForge.Data.Infrastructure.IDbConnectionFactory>(_ =>
            new SqlScopedResolutionDbConnectionFactory(
                _.GetRequiredService<IServiceScopeFactory>(),
                connectionString));

        return services;
    }
}
