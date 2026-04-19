using ArchLucid.Host.Core.Configuration;
using ArchLucid.Host.Core.Jobs;
using ArchLucid.KnowledgeGraph.Interfaces;
using ArchLucid.Persistence.Audit;
using ArchLucid.Persistence.Cosmos;
using ArchLucid.Persistence.Data.Repositories;

using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ArchLucid.Host.Composition.Startup;

public static partial class ServiceCollectionExtensions
{
    /// <summary>
    /// Optional Cosmos polyglot overrides (graph snapshots, agent traces, audit) — runs after SQL/InMemory defaults
    /// and coordinator agent trace registration so the last registration wins.
    /// </summary>
    private static void RegisterCosmosPolyglotPersistence(IServiceCollection services, IConfiguration configuration)
    {
        ArchLucidOptions archOpts = ArchLucidConfigurationBridge.ResolveArchLucidOptions(configuration);
        CosmosDbOptions cosmosSnapshot =
            configuration.GetSection(CosmosDbOptions.SectionName).Get<CosmosDbOptions>() ?? new CosmosDbOptions();

        if (!cosmosSnapshot.AnyCosmosFeatureEnabled)
            return;

        services.AddSingleton<CosmosClientFactory>();

        bool inMemory = ArchLucidOptions.EffectiveIsInMemory(archOpts.StorageProvider);

        if (cosmosSnapshot.GraphSnapshotsEnabled)
        {
            if (inMemory)
                services.AddSingleton<IGraphSnapshotRepository, CosmosGraphSnapshotRepository>();
            else
                services.AddScoped<IGraphSnapshotRepository, CosmosGraphSnapshotRepository>();
        }

        if (cosmosSnapshot.AgentTracesEnabled)
        {
            if (inMemory)
                services.AddSingleton<IAgentExecutionTraceRepository, CosmosAgentExecutionTraceRepository>();
            else
                services.AddScoped<IAgentExecutionTraceRepository, CosmosAgentExecutionTraceRepository>();
        }

        if (!cosmosSnapshot.AuditEventsEnabled)
            return;

        if (inMemory)
            services.AddSingleton<IAuditRepository, CosmosAuditRepository>();
        else
            services.AddScoped<IAuditRepository, CosmosAuditRepository>();

        services.TryAddSingleton<IAuditEventChangeFeedHandler, NoOpAuditEventChangeFeedHandler>();

        if (!ArchLucidJobsOffload.IsOffloaded(configuration, ArchLucidJobNames.AuditChangeFeed))
        {
            services.AddHostedService<AuditEventChangeFeedHostedService>();
        }

        services.AddSingleton<AuditEventChangeFeedSingleBatchProcessor>();
        services.AddSingleton<IAuditEventChangeFeedSingleBatchRunner>(
            static sp => sp.GetRequiredService<AuditEventChangeFeedSingleBatchProcessor>());
        services.AddSingleton<IArchLucidJob, AuditEventChangeFeedArchLucidJob>();
    }
}
