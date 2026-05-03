using ArchLucid.Application.Advisory;
using ArchLucid.Application.Audit;
using ArchLucid.ArtifactSynthesis.Interfaces;
using ArchLucid.ArtifactSynthesis.Repositories;
using ArchLucid.ContextIngestion.Interfaces;
using ArchLucid.ContextIngestion.Repositories;
using ArchLucid.Contracts.Abstractions.Evolution;
using ArchLucid.Contracts.Abstractions.ProductLearning;
using ArchLucid.Contracts.Abstractions.ProductLearning.Planning;
using ArchLucid.Core.AdminNotifications;
using ArchLucid.Core.Authority;
using ArchLucid.Core.Billing;
using ArchLucid.Core.CustomerSuccess;
using ArchLucid.Core.Feedback;
using ArchLucid.Core.Concurrency;
using ArchLucid.Core.GoToMarket;
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
using ArchLucid.Host.Composition.GoToMarket;
using ArchLucid.Host.Core.DataConsistency;
using ArchLucid.Host.Core.Hosted;
using ArchLucid.Host.Core.Jobs;
using ArchLucid.KnowledgeGraph.Interfaces;
using ArchLucid.KnowledgeGraph.Repositories;
using ArchLucid.Persistence;
using ArchLucid.Persistence.Archival;
using ArchLucid.Persistence.Audit;
using ArchLucid.Persistence.Marketing;
using ArchLucid.Persistence.Billing;
using ArchLucid.Persistence.CustomerSuccess;
using ArchLucid.Persistence.Feedback;
using ArchLucid.Persistence.Findings;
using ArchLucid.Persistence.Conversation;
using ArchLucid.Persistence.Coordination.Compare;
using ArchLucid.Persistence.Coordination.Diagnostics;
using ArchLucid.Persistence.Coordination.Evolution;
using ArchLucid.Persistence.Coordination.ProductLearning;
using ArchLucid.Persistence.Coordination.ProductLearning.Planning;
using ArchLucid.Persistence.Coordination.Replay;
using ArchLucid.Persistence.Coordination.Retrieval;
using ArchLucid.Persistence.Data.Infrastructure;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Governance;
using ArchLucid.Persistence.Identity;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Orchestration;
using ArchLucid.Persistence.Orchestration.Pipeline;
using ArchLucid.Persistence.Provenance;
using ArchLucid.Persistence.Queries;
using ArchLucid.Persistence.Repositories;
using ArchLucid.Persistence.Telemetry;
using ArchLucid.Core.Scim;
using ArchLucid.Persistence.AdminNotifications;
using ArchLucid.Persistence.Scim;
using ArchLucid.Persistence.Tenancy;
using ArchLucid.Persistence.Value;
using ArchLucid.Persistence.Tenancy.Diagnostics;
using ArchLucid.Persistence.Transactions;
using ArchLucid.Persistence.Pilots;
using ArchLucid.Provenance;

namespace ArchLucid.Host.Composition.Configuration;

internal sealed class InMemoryStorageProviderRegistrar : IStorageProviderRegistrar
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IContextSnapshotRepository, InMemoryContextSnapshotRepository>();
        services.AddSingleton<IGraphSnapshotRepository, InMemoryGraphSnapshotRepository>();
        services.AddSingleton<IFindingsSnapshotRepository, InMemoryFindingsSnapshotRepository>();
        services.AddSingleton<IFindingInspectReadRepository>(sp =>
            new InMemoryFindingInspectReadRepository(sp.GetRequiredService<IAuthorityQueryService>()));
        services.AddSingleton<IDecisionTraceRepository, InMemoryDecisionTraceRepository>();
        services.AddSingleton<IGoldenManifestRepository, InMemoryGoldenManifestRepository>();
        services.AddSingleton<IArtifactBundleRepository, InMemoryArtifactBundleRepository>();
        services.AddSingleton<ITenantRepository, InMemoryTenantRepository>();
        services.AddSingleton<IScimTenantTokenRepository, InMemoryScimTenantTokenRepository>();
        services.AddSingleton<IScimUserRepository, InMemoryScimUserRepository>();
        services.AddSingleton<IAdminNotificationsRepository, NoOpAdminNotificationsRepository>();
        services.AddSingleton<IScimGroupRepository, InMemoryScimGroupRepository>();
        services.AddSingleton<IRoiBulletinAggregateReader, InMemoryRoiBulletinAggregateReader>();
        services.AddSingleton<IReferenceEvidenceRunLookup, InMemoryReferenceEvidenceRunLookup>();
        services.AddSingleton<ITenantNotificationChannelPreferencesRepository, InMemoryTenantNotificationChannelPreferencesRepository>();
        services.AddSingleton<ITenantTeamsIncomingWebhookConnectionRepository, InMemoryTenantTeamsIncomingWebhookConnectionRepository>();
        services.AddSingleton<ITenantExecDigestPreferencesRepository, InMemoryTenantExecDigestPreferencesRepository>();
        services.AddSingleton<ITenantHardPurgeService, NoOpTenantHardPurgeService>();
        services.AddSingleton<IBillingLedger, InMemoryBillingLedger>();
        services.AddSingleton<ITenantCustomerSuccessRepository, InMemoryTenantCustomerSuccessRepository>();
        services.AddSingleton<ICorePilotTeamChecklistRepository, InMemoryCorePilotTeamChecklistRepository>();
        services.AddSingleton<IOperatorStickinessSnapshotReader, InMemoryOperatorStickinessSnapshotReader>();
        services.AddSingleton<IFindingFeedbackRepository, InMemoryFindingFeedbackRepository>();
        services.AddSingleton<IFindingReviewTrailRepository, NoOpFindingReviewTrailRepository>();
        services.AddSingleton<IImportedArchitectureRequestRepository, NoOpImportedArchitectureRequestRepository>();
        services.AddSingleton<ITrialIdentityUserRepository, InMemoryNoTrialIdentityUserRepository>();
        services.AddSingleton<IRunRepository>(sp =>
            new InMemoryRunRepository(sp.GetRequiredService<ITenantRepository>()));
        services.AddSingleton<ICommittedArchitectureReviewFlagReader, RunRepositoryCommittedArchitectureReviewFlagReader>();
        services.AddSingleton<IAuthorityQueryService, InMemoryAuthorityQueryService>();
        services.AddSingleton<IArtifactQueryService, InMemoryArtifactQueryService>();
        services.AddScoped<IAuthorityCompareService, AuthorityCompareService>();
        services.AddScoped<IAuthorityReplayService, AuthorityReplayService>();
        services.AddSingleton<IAuditRepository, InMemoryAuditRepository>();
        services.AddSingleton<IPilotScorecardMetricsReader, NullPilotScorecardMetricsReader>();
        services.AddSingleton<IPilotBaselineRepository, InMemoryPilotBaselineRepository>();
        services.AddSingleton<IValueReportMetricsReader, InMemoryValueReportMetricsReader>();
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
        services.AddSingleton<IMarketingPricingQuoteRequestRepository, NoOpMarketingPricingQuoteRequestRepository>();
        services.AddSingleton<IFirstTenantFunnelEventStore, NoopFirstTenantFunnelEventStore>();

        ArchLucidStorageServiceCollectionExtensions.RegisterHostLeaderLeaseInfrastructure(services);
        services.AddSingleton<IHostLeaderLeaseRepository, NoOpHostLeaderLeaseRepository>();

        ArchLucidStorageServiceCollectionExtensions.RegisterArtifactLargePayloadBlobStore(services, configuration);
        ArchLucidStorageServiceCollectionExtensions.RegisterHotPathReadCaching(services, configuration);
        ArchLucidStorageServiceCollectionExtensions.RegisterSharedDistributedCacheAndLlmCompletion(services, configuration);

        services.AddSingleton<IOutboxOperationalMetricsReader, InMemoryOutboxOperationalMetricsReader>();
        services.AddSingleton<ITrialFunnelOperationalMetricsReader, InMemoryTrialFunnelOperationalMetricsReader>();
        services.AddScoped<ITrialFunnelCommitHook, SqlTrialFunnelCommitHook>();
        // In-memory hosts intentionally omit ISqlConnectionFactory; first-session SQL persistence is not modeled here.
        services.AddSingleton<IFirstSessionLifecycleHook>(NoOpFirstSessionLifecycleHook.Instance);

        services.AddHostedService<OutboxOperationalMetricsHostedService>();

        // Parity with Sql path: orphan probe resolves but no-ops when storage is InMemory (see DataConsistencyOrphanProbeExecutor).
        // IDbConnectionFactory stays UnsupportedRelationalDbConnectionFactory so DAST/ZAP containers need no SQL connection string.
        services.AddSingleton<IDbConnectionFactory, UnsupportedRelationalDbConnectionFactory>();
        services.AddSingleton<DataConsistencyOrphanProbeExecutor>();
        services.AddSingleton<IDataConsistencyOrphanProbeExecutor>(
            static sp => sp.GetRequiredService<DataConsistencyOrphanProbeExecutor>());
        services.AddSingleton<IArchLucidJob, OrphanProbeArchLucidJob>();

        if (!ArchLucidJobsOffload.IsOffloaded(configuration, ArchLucidJobNames.OrphanProbe))

            services.AddHostedService<DataConsistencyOrphanProbeHostedService>();

    }
}
