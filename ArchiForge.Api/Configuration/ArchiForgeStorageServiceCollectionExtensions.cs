using ArchiForge.Api.Configuration;
using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Repositories;
using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.ContextIngestion.Repositories;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Repositories;
using ArchiForge.KnowledgeGraph.Interfaces;
using ArchiForge.KnowledgeGraph.Repositories;
using ArchiForge.Persistence.Audit;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.Interfaces;
using ArchiForge.Persistence.Compare;
using ArchiForge.Persistence.Orchestration;
using ArchiForge.Persistence.Queries;
using ArchiForge.Persistence.Replay;
using ArchiForge.Persistence.Repositories;
using ArchiForge.Persistence.Sql;
using ArchiForge.Persistence.Transactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

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
            return services;
        }

        var connectionString = configuration.GetConnectionString("ArchiForge")
            ?? throw new InvalidOperationException("Missing connection string 'ArchiForge'.");

        services.AddSingleton<ISqlConnectionFactory>(_ =>
            new SqlConnectionFactory(connectionString));

        var persistenceAssembly = typeof(SqlSchemaBootstrapper).Assembly;
        var dir = Path.GetDirectoryName(persistenceAssembly.Location) ?? AppContext.BaseDirectory;
        var scriptPath = Path.Combine(dir, "Scripts", "001_AuthorityStore.sql");

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

        return services;
    }
}
