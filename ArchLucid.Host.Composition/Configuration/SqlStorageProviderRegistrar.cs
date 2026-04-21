using System.Reflection;

using ArchLucid.Application.Audit;
using ArchLucid.ArtifactSynthesis.Interfaces;
using ArchLucid.ContextIngestion.Interfaces;
using ArchLucid.Contracts.Abstractions.Evolution;
using ArchLucid.Contracts.Abstractions.ProductLearning;
using ArchLucid.Contracts.Abstractions.ProductLearning.Planning;
using ArchLucid.Core.Authority;
using ArchLucid.Core.Billing;
using ArchLucid.Core.CustomerSuccess;
using ArchLucid.Core.Concurrency;
using ArchLucid.Core.Identity;
using ArchLucid.Core.Tenancy;
using ArchLucid.Core.Transactions;
using ArchLucid.Decisioning.Advisory.Delivery;
using ArchLucid.Decisioning.Advisory.Learning;
using ArchLucid.Decisioning.Advisory.Workflow;
using ArchLucid.Decisioning.Alerts;
using ArchLucid.Decisioning.Alerts.Composite;
using ArchLucid.Decisioning.Alerts.Delivery;
using ArchLucid.Decisioning.Governance.PolicyPacks;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Host.Core.Authority;
using ArchLucid.Host.Core.Configuration;
using ArchLucid.Host.Core.DataAccess;
using ArchLucid.Host.Core.DataConsistency;
using ArchLucid.Host.Core.Hosted;
using ArchLucid.Host.Core.Jobs;
using ArchLucid.KnowledgeGraph.Interfaces;
using ArchLucid.Persistence;
using ArchLucid.Persistence.Archival;
using ArchLucid.Persistence.Configuration;
using ArchLucid.Persistence.Audit;
using ArchLucid.Persistence.Billing;
using ArchLucid.Persistence.CustomerSuccess;
using ArchLucid.Persistence.Concurrency;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Conversation;
using ArchLucid.Persistence.Coordination.Compare;
using ArchLucid.Persistence.Coordination.Evolution;
using ArchLucid.Persistence.Coordination.ProductLearning;
using ArchLucid.Persistence.Coordination.ProductLearning.Planning;
using ArchLucid.Persistence.Coordination.Replay;
using ArchLucid.Persistence.Coordination.Retrieval;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Governance;
using ArchLucid.Persistence.Identity;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Orchestration;
using ArchLucid.Persistence.Orchestration.Pipeline;
using ArchLucid.Persistence.Provenance;
using ArchLucid.Persistence.Queries;
using ArchLucid.Persistence.Repositories;
using ArchLucid.Persistence.Sql;
using ArchLucid.Persistence.Tenancy;
using ArchLucid.Persistence.Value;
using ArchLucid.Persistence.Tenancy.Diagnostics;
using ArchLucid.Persistence.Transactions;
using ArchLucid.Provenance;

using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Composition.Configuration;

internal sealed class SqlStorageProviderRegistrar : IStorageProviderRegistrar
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        DapperGlobalCommandTimeoutBootstrap.ApplyIfConfigured(configuration);

        string connectionString = ArchLucidConfigurationBridge.ResolveSqlConnectionString(configuration)
                                  ?? throw new InvalidOperationException(
                                      "Missing connection string 'ArchLucid'.");

        services.Configure<SqlServerOptions>(configuration.GetSection(SqlServerOptions.SectionName));

        ArchLucidStorageServiceCollectionExtensions.RegisterArtifactLargePayloadBlobStore(services, configuration);
        ArchLucidStorageServiceCollectionExtensions.RegisterHotPathReadCaching(services, configuration);
        ArchLucidStorageServiceCollectionExtensions.RegisterSharedDistributedCacheAndLlmCompletion(services, configuration);

        services.AddSingleton<SqlConnectionFactory>(
            _ => new SqlConnectionFactory(connectionString));
        services.AddSingleton<ResilientSqlConnectionFactory>(sp =>
        {
            SqlOpenResilienceOptions sqlOpenOpts = sp.GetRequiredService<IOptions<SqlOpenResilienceOptions>>().Value;

            return new ResilientSqlConnectionFactory(
                sp.GetRequiredService<SqlConnectionFactory>(),
                SqlOpenResilienceDefaults.BuildSqlOpenRetryPipeline(
                    sp.GetRequiredService<ILogger<ResilientSqlConnectionFactory>>(),
                    sqlOpenOpts.MaxRetryAttempts,
                    TimeSpan.FromMilliseconds(sqlOpenOpts.BaseDelayMilliseconds)));
        });

        services.AddScoped<IRlsSessionContextApplicator, RlsSessionContextApplicator>();
        services.AddScoped<ISqlConnectionFactory>(sp =>
        {
            SqlServerOptions sqlOpts =
                sp.GetRequiredService<IOptionsMonitor<SqlServerOptions>>().CurrentValue;
            ResilientSqlConnectionFactory resilient = sp.GetRequiredService<ResilientSqlConnectionFactory>();

            if (!sqlOpts.RowLevelSecurity.ApplySessionContext)
                return resilient;

            return new SessionContextSqlConnectionFactory(
                resilient,
                sp.GetRequiredService<IRlsSessionContextApplicator>(),
                sp.GetRequiredService<ILogger<SessionContextSqlConnectionFactory>>());
        });

        services.AddScoped<IAuthorityRunListConnectionFactory>(sp => new ReadReplicaRoutedConnectionFactory(
            sp.GetRequiredService<ResilientSqlConnectionFactory>(),
            sp.GetRequiredService<IOptionsMonitor<SqlServerOptions>>(),
            sp.GetRequiredService<IRlsSessionContextApplicator>(),
            ReadReplicaQueryRoute.AuthorityRunList));

        services.AddScoped<IGovernanceResolutionReadConnectionFactory>(sp => new ReadReplicaRoutedConnectionFactory(
            sp.GetRequiredService<ResilientSqlConnectionFactory>(),
            sp.GetRequiredService<IOptionsMonitor<SqlServerOptions>>(),
            sp.GetRequiredService<IRlsSessionContextApplicator>(),
            ReadReplicaQueryRoute.GovernanceResolution));

        services.AddScoped<IGoldenManifestLookupReadConnectionFactory>(sp => new ReadReplicaRoutedConnectionFactory(
            sp.GetRequiredService<ResilientSqlConnectionFactory>(),
            sp.GetRequiredService<IOptionsMonitor<SqlServerOptions>>(),
            sp.GetRequiredService<IRlsSessionContextApplicator>(),
            ReadReplicaQueryRoute.GoldenManifestLookup));

        Assembly persistenceAssembly = typeof(SqlSchemaBootstrapper).Assembly;
        string dir = Path.GetDirectoryName(persistenceAssembly.Location) ?? AppContext.BaseDirectory;
        string scriptPath = Path.Combine(dir, "Scripts", "ArchLucid.sql");

        services.AddScoped<ISchemaBootstrapper>(sp =>
            new SqlSchemaBootstrapper(
                sp.GetRequiredService<ISqlConnectionFactory>(),
                scriptPath));

        services.AddScoped<IContextSnapshotRepository, SqlContextSnapshotRepository>();
        services.AddScoped<IGraphSnapshotRepository, SqlGraphSnapshotRepository>();
        services.AddScoped<IFindingsSnapshotRepository, SqlFindingsSnapshotRepository>();
        services.AddScoped<IDecisionTraceRepository, SqlDecisionTraceRepository>();
        ArchLucidStorageServiceCollectionExtensions.RegisterGoldenManifestRunAndPolicyPackRepositories(services, configuration);

        services.AddScoped<IArtifactBundleRepository, SqlArtifactBundleRepository>();
        services.AddScoped<IAuthorityQueryService, DapperAuthorityQueryService>();
        services.AddScoped<IArtifactQueryService, DapperArtifactQueryService>();
        services.AddScoped<IAuthorityCompareService, AuthorityCompareService>();
        services.AddScoped<IAuthorityReplayService, AuthorityReplayService>();
        services.AddScoped<IArchLucidUnitOfWorkFactory, DapperArchLucidUnitOfWorkFactory>();
        services.AddScoped<IDistributedCreateRunIdempotencyLock, SqlSessionDistributedCreateRunIdempotencyLock>();
        services.AddScoped<IRetrievalIndexingOutboxRepository, DapperRetrievalIndexingOutboxRepository>();
        services.AddScoped<IIntegrationEventOutboxRepository, DapperIntegrationEventOutboxRepository>();
        services.AddScoped<IProductLearningPilotSignalRepository, DapperProductLearningPilotSignalRepository>();
        services.AddScoped<IProductLearningPlanningRepository, DapperProductLearningPlanningRepository>();
        services.AddScoped<IImprovementThemeExtractionService, ImprovementThemeExtractionService>();
        services.AddScoped<IImprovementPlanningService, ImprovementPlanningService>();
        services.AddScoped<ICandidateChangeSetService, CandidateChangeSetService>();
        services.AddScoped<IImprovementPlanPrioritizationService, ImprovementPlanPrioritizationService>();
        services.AddScoped<IProductLearningFeedbackAggregationService, ProductLearningFeedbackAggregationService>();
        services.AddScoped<IProductLearningImprovementOpportunityService, ProductLearningImprovementOpportunityService>();
        services.AddScoped<IProductLearningDashboardService, ProductLearningDashboardService>();
        services.AddScoped<IEvolutionCandidateChangeSetRepository, DapperEvolutionCandidateChangeSetRepository>();
        services.AddScoped<IEvolutionSimulationRunRepository, DapperEvolutionSimulationRunRepository>();
        services.AddScoped<IAuthorityPipelineWorkRepository, DapperAuthorityPipelineWorkRepository>();
        services.AddScoped<IAsyncAuthorityPipelineModeResolver, FeatureManagementAuthorityPipelineModeResolver>();
        services.AddScoped<IAuthorityPipelineStagesExecutor, AuthorityPipelineStagesExecutor>();
        services.AddScoped<IAuthorityRunOrchestrator, AuthorityRunOrchestrator>();
        services.AddScoped<IAuditRepository, DapperAuditRepository>();
        services.AddScoped<IValueReportMetricsReader, DapperValueReportMetricsReader>();
        services.AddScoped<IRunPipelineAuditTimelineService, RunPipelineAuditTimelineService>();
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
        services.AddScoped<IPolicyPackChangeLogRepository, DapperPolicyPackChangeLogRepository>();
        services.AddScoped<IDataArchivalCoordinator, DataArchivalCoordinator>();
        services.AddScoped<ITenantRepository, DapperTenantRepository>();
        services.AddScoped<ITenantCustomerSuccessRepository, SqlTenantCustomerSuccessRepository>();
        services.AddScoped<ITenantNotificationChannelPreferencesRepository, DapperTenantNotificationChannelPreferencesRepository>();
        services.AddScoped<ITenantExecDigestPreferencesRepository, DapperTenantExecDigestPreferencesRepository>();
        services.AddScoped<ITenantHardPurgeService, SqlTenantHardPurgeService>();
        services.AddScoped<IBillingLedger, SqlBillingLedger>();
        services.AddScoped<ITrialIdentityUserRepository, SqlTrialIdentityUserRepository>();
        services.AddScoped<IUsageEventRepository, DapperUsageEventRepository>();
        services.AddScoped<IReferenceEvidenceRunLookup, SqlReferenceEvidenceRunLookup>();

        services.AddSingleton<Persistence.Data.Infrastructure.IDbConnectionFactory>(p =>
            new SqlScopedResolutionDbConnectionFactory(
                p.GetRequiredService<IServiceScopeFactory>(),
                connectionString));

        ArchLucidStorageServiceCollectionExtensions.RegisterHostLeaderLeaseInfrastructure(services);
        services.AddSingleton<IHostLeaderLeaseRepository, SqlHostLeaderLeaseRepository>();

        // Scoped: DapperTrialFunnelOperationalMetricsReader takes ISqlConnectionFactory (scoped); hosted service resolves it per scope.
        services.AddScoped<ITrialFunnelOperationalMetricsReader, DapperTrialFunnelOperationalMetricsReader>();
        services.AddScoped<ITrialFunnelCommitHook, SqlTrialFunnelCommitHook>();
        services.AddScoped<ITenantOnboardingStateRepository, SqlTenantOnboardingStateRepository>();
        services.AddScoped<IFirstSessionLifecycleHook, SqlFirstSessionLifecycleHook>();

        services.AddHostedService<OutboxOperationalMetricsHostedService>();

        services.AddSingleton<DataConsistencyOrphanProbeExecutor>();
        services.AddSingleton<IDataConsistencyOrphanProbeExecutor>(
            static sp => sp.GetRequiredService<DataConsistencyOrphanProbeExecutor>());
        services.AddSingleton<IArchLucidJob, OrphanProbeArchLucidJob>();

        if (!ArchLucidJobsOffload.IsOffloaded(configuration, ArchLucidJobNames.OrphanProbe))

            services.AddHostedService<DataConsistencyOrphanProbeHostedService>();

    }
}
