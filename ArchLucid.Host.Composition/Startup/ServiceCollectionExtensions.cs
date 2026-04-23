using ArchLucid.Application.Bootstrap;
using ArchLucid.Core.Configuration;
using ArchLucid.Host.Composition.Configuration;
using ArchLucid.Host.Core.Configuration;
using ArchLucid.Host.Core.Hosting;
using ArchLucid.Persistence.Archival;

namespace ArchLucid.Host.Composition.Startup;

/// <summary>
/// Composition root for ArchLucid application services. Registration is split across partial files by subsystem
/// (scheduling, data plane, jobs, pipeline, coordinator, agents, decisioning).
/// </summary>
public static partial class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers ArchLucid domain services, persistence choices, hosted workers, and health checks for the given host role.
    /// </summary>
    public static IServiceCollection AddArchLucidApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration,
        ArchLucidHostingRole hostingRole)
    {
        services.AddSingleton(TimeProvider.System);
        services.Configure<DemoOptions>(configuration.GetSection(DemoOptions.SectionName));
        services.AddScoped<ArchLucid.Application.Authority.IAuthorityCommittedManifestChainWriter,
            ArchLucid.Application.Authority.AuthorityCommittedManifestChainWriter>();
        RegisterAzureOpenAiCircuitBreakerOptions(services, configuration);
        services.Configure<BatchReplayOptions>(configuration.GetSection(BatchReplayOptions.SectionName));
        services.Configure<ApiDeprecationOptions>(configuration.GetSection(ApiDeprecationOptions.SectionName));
        services.Configure<DataArchivalOptions>(configuration.GetSection(DataArchivalOptions.SectionName));
        services.Configure<HostLeaderElectionOptions>(configuration.GetSection(HostLeaderElectionOptions.SectionName));
        services.AddScoped<IDemoSeedService, DemoSeedService>();
        services.AddArchLucidFeatureManagement(configuration);
        services.AddArchLucidStorage(configuration);
        RegisterTenancyMeteringAndSecrets(services, configuration);
        RegisterBilling(services, configuration);
        RegisterAdvisoryScheduling(services, configuration, hostingRole);
        RegisterExecDigestServices(services);
        RegisterTrialLifecycleEmailHostedServices(services, configuration, hostingRole);
        RegisterExecDigestWorkerInfrastructure(services, configuration, hostingRole);
        RegisterTrialLifecycleScheduler(services, configuration, hostingRole);
        RegisterTrialArchitecturePreseed(services, configuration, hostingRole);
        RegisterTenantHealthScoring(services, configuration, hostingRole);
        RegisterDigestDelivery(services, configuration);
        RegisterIntegrationEventPublishing(services, configuration);
        RegisterTrialLifecycleAuditEmailPublishing(services);
        RegisterAlerts(services);
        RegisterBackgroundJobs(services, configuration, hostingRole);
        RegisterRunExportAndArchitectureAnalysis(services, configuration);
        RegisterComparisonReplayAndDrift(services, configuration);
        RegisterRunReplayManifestAndDiffs(services, configuration);
        RegisterContextIngestionAndKnowledgeGraph(services);
        RegisterDecisioningEngines(services, configuration);
        RegisterCoordinatorDecisionEngineAndRepositories(services, configuration);
        RegisterArtifactSynthesis(services);
        RegisterAgentExecution(services, configuration);
        RegisterRetrieval(services, configuration);
        RegisterGovernance(services, configuration);
        RegisterRetrievalIndexingOutbox(services, hostingRole);
        RegisterIntegrationEventOutbox(services, hostingRole);
        RegisterIntegrationEventConsumer(services, configuration, hostingRole);
        RegisterDataArchivalHostedService(services, configuration, hostingRole);
        RegisterArchLucidHealthChecks(services, configuration, hostingRole);
        RegisterCosmosPolyglotPersistence(services, configuration);
        RegisterArchLucidJobRunners(services, configuration);

        return services;
    }
}
