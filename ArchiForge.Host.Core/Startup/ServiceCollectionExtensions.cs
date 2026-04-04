using ArchiForge.Application.Bootstrap;
using ArchiForge.Host.Core.Configuration;
using ArchiForge.Host.Core.Hosting;
using ArchiForge.Persistence.Archival;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArchiForge.Host.Core.Startup;

/// <summary>
/// Composition root for ArchiForge application services. Registration is split across partial files by subsystem
/// (scheduling, data plane, jobs, pipeline, coordinator, agents, decisioning).
/// </summary>
public static partial class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers ArchiForge domain services, persistence choices, hosted workers, and health checks for the given host role.
    /// </summary>
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
}
