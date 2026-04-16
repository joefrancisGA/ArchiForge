using ArchLucid.AgentRuntime;
using ArchLucid.Core.Authority;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Governance.PolicyPacks;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Host.Core.Configuration;
using ArchLucid.Host.Core.Hosted;
using ArchLucid.Persistence.BlobStore;
using ArchLucid.Persistence.Caching;
using ArchLucid.Persistence.Coordination.Caching;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Governance;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Repositories;
using ArchLucid.Persistence.Sql;

using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Composition.Configuration;

public static class ArchLucidStorageServiceCollectionExtensions
{
    public static IServiceCollection AddArchLucidStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArchLucidOptions options = ArchLucidConfigurationBridge.ResolveArchLucidOptions(configuration);

        services.Configure<SqlOpenResilienceOptions>(configuration.GetSection(SqlOpenResilienceOptions.SectionName));
        services.PostConfigure<SqlOpenResilienceOptions>(static o => o.Normalize());

        services.Configure<AuthorityPipelineOptions>(configuration.GetSection(AuthorityPipelineOptions.SectionName));

        services.Configure<DataConsistencyProbeOptions>(
            configuration.GetSection(DataConsistencyProbeOptions.SectionName));

        services.AddOptions<ArchLucidOptions>()
            .Configure<IConfiguration>(
                static (opts, cfg) =>
                {
                    ArchLucidOptions resolved = ArchLucidConfigurationBridge.ResolveArchLucidOptions(cfg);
                    opts.StorageProvider = resolved.StorageProvider;
                });

        IStorageProviderRegistrar registrar = ArchLucidOptions.EffectiveIsInMemory(options.StorageProvider)
            ? new InMemoryStorageProviderRegistrar()
            : new SqlStorageProviderRegistrar();

        registrar.Register(services, configuration);

        return services;
    }

    /// <summary>
    /// LLM completion cache + response store — same for Sql and InMemory storage (after Sql-only hot-path cache when applicable).
    /// </summary>
    internal static void RegisterSharedDistributedCacheAndLlmCompletion(
        IServiceCollection services,
        IConfiguration configuration)
    {
        RegisterDistributedCacheForLlmCompletionIfNeeded(services, configuration);
        RegisterLlmCompletionResponseStore(services, configuration);
    }

    internal static void RegisterHostLeaderLeaseInfrastructure(IServiceCollection services)
    {
        services.AddSingleton<HostInstanceIdentifier>();
        services.AddSingleton<HostLeaderElectionCoordinator>();
    }

    internal static void RegisterDistributedCacheForLlmCompletionIfNeeded(
        IServiceCollection services,
        IConfiguration configuration)
    {
        LlmCompletionResponseCacheOptions llm =
            configuration.GetSection(LlmCompletionResponseCacheOptions.SectionName).Get<LlmCompletionResponseCacheOptions>()
            ?? new LlmCompletionResponseCacheOptions();

        if (!llm.Enabled || !string.Equals(llm.Provider, "Distributed", StringComparison.OrdinalIgnoreCase))
            return;

        if (services.Any(static d => d.ServiceType == typeof(IDistributedCache)))
            return;

        HotPathCacheOptions hotPath =
            configuration.GetSection(HotPathCacheOptions.SectionName).Get<HotPathCacheOptions>() ??
            new HotPathCacheOptions();

        string redis = string.IsNullOrWhiteSpace(llm.RedisConnectionString)
            ? hotPath.RedisConnectionString.Trim()
            : llm.RedisConnectionString.Trim();

        if (string.IsNullOrEmpty(redis))
        {
            throw new InvalidOperationException(
                "LlmCompletionCache:Provider is Distributed but no IDistributedCache is registered and neither LlmCompletionCache:RedisConnectionString nor HotPathCache:RedisConnectionString is set.");
        }

        services.AddStackExchangeRedisCache(o => o.Configuration = redis);
    }

    internal static void RegisterLlmCompletionResponseStore(IServiceCollection services, IConfiguration configuration)
    {
        LlmCompletionResponseCacheOptions llm =
            configuration.GetSection(LlmCompletionResponseCacheOptions.SectionName).Get<LlmCompletionResponseCacheOptions>()
            ?? new LlmCompletionResponseCacheOptions();

        if (!llm.Enabled)
            return;

        if (string.Equals(llm.Provider, "Distributed", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<ILlmCompletionResponseStore>(sp =>
                new DistributedLlmCompletionResponseStore(sp.GetRequiredService<IDistributedCache>()));

            return;
        }

        int maxEntries = Math.Max(1, llm.MaxEntries);
        services.AddSingleton<ILlmCompletionResponseStore>(_ => new MemoryLlmCompletionResponseStore(maxEntries));
    }

    internal static void RegisterHotPathReadCaching(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<HotPathCacheOptions>(
            configuration.GetSection(HotPathCacheOptions.SectionName));

        HotPathCacheOptions snapshot = configuration
                                           .GetSection(HotPathCacheOptions.SectionName)
                                           .Get<HotPathCacheOptions>()
                                       ?? new HotPathCacheOptions();

        if (!snapshot.Enabled)
            return;

        string provider = HotPathCacheProviderResolver.ResolveEffectiveProvider(snapshot);

        if (string.Equals(provider, "Redis", StringComparison.OrdinalIgnoreCase))
        {
            string redis = snapshot.RedisConnectionString.Trim();

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

    internal static void RegisterGoldenManifestRunAndPolicyPackRepositories(
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

    internal static void RegisterArtifactLargePayloadBlobStore(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ArtifactLargePayloadOptions>(
            configuration.GetSection(ArtifactLargePayloadOptions.SectionName));

        ArtifactLargePayloadOptions snapshot = configuration
                                                   .GetSection(ArtifactLargePayloadOptions.SectionName)
                                                   .Get<ArtifactLargePayloadOptions>()
                                               ?? new ArtifactLargePayloadOptions();

        string provider = snapshot.BlobProvider;

        if (string.Equals(provider, "AzureBlob", StringComparison.OrdinalIgnoreCase))
        {
            string uriText = snapshot.AzureBlobServiceUri;

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
                    sp.GetRequiredService<TokenCredential>(),
                    sp.GetRequiredService<IScopeContextProvider>()));
        }
        else if (string.Equals(provider, "Local", StringComparison.OrdinalIgnoreCase))
        {
            string root = string.IsNullOrWhiteSpace(snapshot.LocalRootPath)
                ? Path.Combine(AppContext.BaseDirectory, "blob-store")
                : snapshot.LocalRootPath;
            services.AddSingleton<IArtifactBlobStore>(sp =>
                new LocalFileArtifactBlobStore(root, sp.GetRequiredService<IScopeContextProvider>()));
        }
        else
        {
            services.AddSingleton<IArtifactBlobStore, NullArtifactBlobStore>();
        }
    }
}
