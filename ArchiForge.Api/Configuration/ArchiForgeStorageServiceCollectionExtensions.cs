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
using ArchiForge.Persistence.Provenance;
using ArchiForge.Persistence.Queries;
using ArchiForge.Persistence.Replay;
using ArchiForge.Persistence.Repositories;
using ArchiForge.Persistence.Sql;
using ArchiForge.Persistence.Transactions;
using ArchiForge.Provenance;

namespace ArchiForge.Api.Configuration;

public static class ArchiForgeStorageServiceCollectionExtensions
{
    public static IServiceCollection AddArchiForgeStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
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
            services.AddScoped<IAuthorityRunOrchestrator, AuthorityRunOrchestrator>();
            return services;
        }

        var connectionString = configuration.GetConnectionString("ArchiForge")
            ?? throw new InvalidOperationException("Missing connection string 'ArchiForge'.");

        services.AddSingleton<ISqlConnectionFactory>(_ =>
            new SqlConnectionFactory(connectionString));

        var persistenceAssembly = typeof(SqlSchemaBootstrapper).Assembly;
        var dir = Path.GetDirectoryName(persistenceAssembly.Location) ?? AppContext.BaseDirectory;
        var scriptPath = Path.Combine(dir, "Scripts", "ArchiForge.sql");

        services.AddSingleton<ISchemaBootstrapper>(sp =>
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

        return services;
    }
}
