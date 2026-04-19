using ArchLucid.Application.Audit;
using ArchLucid.ArtifactSynthesis.Interfaces;
using ArchLucid.ArtifactSynthesis.Repositories;
using ArchLucid.ContextIngestion.Interfaces;
using ArchLucid.ContextIngestion.Repositories;
using ArchLucid.Contracts.Evolution;
using ArchLucid.Contracts.ProductLearning;
using ArchLucid.Contracts.ProductLearning.Planning;
using ArchLucid.Core.Authority;
using ArchLucid.Core.Billing;
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
using ArchLucid.Decisioning.Repositories;
using ArchLucid.Host.Core.Hosted;
using ArchLucid.KnowledgeGraph.Interfaces;
using ArchLucid.KnowledgeGraph.Repositories;
using ArchLucid.Persistence;
using ArchLucid.Persistence.Archival;
using ArchLucid.Persistence.Audit;
using ArchLucid.Persistence.Billing;
using ArchLucid.Persistence.Conversation;
using ArchLucid.Persistence.Coordination.Compare;
using ArchLucid.Persistence.Coordination.Diagnostics;
using ArchLucid.Persistence.Coordination.Evolution;
using ArchLucid.Persistence.Coordination.ProductLearning;
using ArchLucid.Persistence.Coordination.ProductLearning.Planning;
using ArchLucid.Persistence.Coordination.Replay;
using ArchLucid.Persistence.Coordination.Retrieval;
using ArchLucid.Persistence.Governance;
using ArchLucid.Persistence.Identity;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Orchestration;
using ArchLucid.Persistence.Orchestration.Pipeline;
using ArchLucid.Persistence.Provenance;
using ArchLucid.Persistence.Queries;
using ArchLucid.Persistence.Repositories;
using ArchLucid.Persistence.Tenancy;
using ArchLucid.Persistence.Tenancy.Diagnostics;
using ArchLucid.Persistence.Transactions;
using ArchLucid.Provenance;

namespace ArchLucid.Host.Composition.Configuration;

internal sealed class InMemoryStorageProviderRegistrar : IStorageProviderRegistrar
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IContextSnapshotRepository, InMemoryContextSnapshotRepository>();
        services.AddSingleton<IGraphSnapshotRepository, InMemoryGraphSnapshotRepository>();
        services.AddSingleton<IFindingsSnapshotRepository, InMemoryFindingsSnapshotRepository>();
        services.AddSingleton<IDecisionTraceRepository, InMemoryDecisionTraceRepository>();
        services.AddSingleton<IGoldenManifestRepository, InMemoryGoldenManifestRepository>();
        services.AddSingleton<IArtifactBundleRepository, InMemoryArtifactBundleRepository>();
        services.AddSingleton<ITenantRepository, InMemoryTenantRepository>();
        services.AddSingleton<ITenantHardPurgeService, NoOpTenantHardPurgeService>();
        services.AddSingleton<IBillingLedger, InMemoryBillingLedger>();
        services.AddSingleton<ITrialIdentityUserRepository, InMemoryNoTrialIdentityUserRepository>();
        services.AddSingleton<IRunRepository>(sp =>
            new InMemoryRunRepository(sp.GetRequiredService<ITenantRepository>()));
        services.AddSingleton<IAuthorityQueryService, InMemoryAuthorityQueryService>();
        services.AddSingleton<IArtifactQueryService, InMemoryArtifactQueryService>();
        services.AddScoped<IAuthorityCompareService, AuthorityCompareService>();
        services.AddScoped<IAuthorityReplayService, AuthorityReplayService>();
        services.AddSingleton<IAuditRepository, InMemoryAuditRepository>();
        services.AddScoped<IRunPipelineAuditTimelineService, RunPipelineAuditTimelineService>();
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
        services.AddSingleton<IPolicyPackChangeLogRepository, InMemoryPolicyPackChangeLogRepository>();
        services.AddSingleton<IArchLucidUnitOfWorkFactory, InMemoryArchLucidUnitOfWorkFactory>();
        services.AddSingleton<IDistributedCreateRunIdempotencyLock, NoOpDistributedCreateRunIdempotencyLock>();
        services.AddSingleton<IRetrievalIndexingOutboxRepository, InMemoryRetrievalIndexingOutboxRepository>();
        services.AddSingleton<IIntegrationEventOutboxRepository, InMemoryIntegrationEventOutboxRepository>();
        services.AddSingleton<IProductLearningPilotSignalRepository, InMemoryProductLearningPilotSignalRepository>();
        services.AddSingleton<IProductLearningPlanningRepository, InMemoryProductLearningPlanningRepository>();
        services.AddSingleton<IImprovementThemeExtractionService, ImprovementThemeExtractionService>();
        services.AddSingleton<IImprovementPlanningService, ImprovementPlanningService>();
        services.AddSingleton<ICandidateChangeSetService, CandidateChangeSetService>();
        services.AddSingleton<IImprovementPlanPrioritizationService, ImprovementPlanPrioritizationService>();
        services.AddSingleton<IProductLearningFeedbackAggregationService, ProductLearningFeedbackAggregationService>();
        services.AddSingleton<IProductLearningImprovementOpportunityService, ProductLearningImprovementOpportunityService>();
        services.AddSingleton<IProductLearningDashboardService, ProductLearningDashboardService>();
        services.AddSingleton<IEvolutionCandidateChangeSetRepository, InMemoryEvolutionCandidateChangeSetRepository>();
        services.AddSingleton<IEvolutionSimulationRunRepository, InMemoryEvolutionSimulationRunRepository>();
        services.AddSingleton<IConversationThreadRepository, InMemoryConversationThreadRepository>();
        services.AddSingleton<IConversationMessageRepository, InMemoryConversationMessageRepository>();
        services.AddSingleton<IAuthorityPipelineWorkRepository, InMemoryAuthorityPipelineWorkRepository>();
        services.AddSingleton<IAsyncAuthorityPipelineModeResolver, DisabledAsyncAuthorityPipelineModeResolver>();
        services.AddScoped<IAuthorityPipelineStagesExecutor, AuthorityPipelineStagesExecutor>();
        services.AddScoped<IAuthorityRunOrchestrator, AuthorityRunOrchestrator>();
        services.AddScoped<IDataArchivalCoordinator, DataArchivalCoordinator>();
        services.AddSingleton<IUsageEventRepository, InMemoryUsageEventRepository>();

        ArchLucidStorageServiceCollectionExtensions.RegisterHostLeaderLeaseInfrastructure(services);
        services.AddSingleton<Persistence.Data.Repositories.IHostLeaderLeaseRepository, Persistence.Data.Repositories.NoOpHostLeaderLeaseRepository>();

        ArchLucidStorageServiceCollectionExtensions.RegisterArtifactLargePayloadBlobStore(services, configuration);
        ArchLucidStorageServiceCollectionExtensions.RegisterHotPathReadCaching(services, configuration);
        ArchLucidStorageServiceCollectionExtensions.RegisterSharedDistributedCacheAndLlmCompletion(services, configuration);

        services.AddSingleton<IOutboxOperationalMetricsReader, InMemoryOutboxOperationalMetricsReader>();
        services.AddSingleton<ITrialFunnelOperationalMetricsReader, InMemoryTrialFunnelOperationalMetricsReader>();
        services.AddScoped<ITrialFunnelCommitHook, SqlTrialFunnelCommitHook>();

        services.AddHostedService<OutboxOperationalMetricsHostedService>();
    }
}
