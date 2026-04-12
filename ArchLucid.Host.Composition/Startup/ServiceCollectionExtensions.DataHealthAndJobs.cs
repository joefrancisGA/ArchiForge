using ArchLucid.Application.Jobs;
using ArchLucid.Host.Core.Configuration;
using ArchLucid.Host.Core.Health;
using ArchLucid.Host.Core.Hosting;
using ArchLucid.Host.Core.Jobs;
using ArchLucid.Persistence.BlobStore;
using ArchLucid.Persistence.Data.Infrastructure;
using ArchLucid.Persistence.Data.Repositories;

using Azure.Core;
using Azure.Storage.Queues;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Composition.Startup;

public static partial class ServiceCollectionExtensions
{
    private static void RegisterDataInfrastructure(IServiceCollection services, IConfiguration configuration)
    {
        ArchLucidOptions mode = ArchLucidConfigurationBridge.ResolveArchLucidOptions(configuration);

        if (!ArchLucidOptions.EffectiveIsInMemory(mode.StorageProvider))
        {
            return;
        }

        services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
    }

    private static void RegisterArchLucidHealthChecks(
        IServiceCollection services,
        IConfiguration configuration,
        ArchLucidHostingRole hostingRole)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        IHealthChecksBuilder builder = services.AddHealthChecks()
            .AddCheck(
                "liveness",
                () => HealthCheckResult.Healthy("ArchLucid API process is running."),
                tags: [ReadinessTags.Live])
            .AddCheck<SqlConnectionHealthCheck>(
                "database",
                failureStatus: HealthStatus.Unhealthy,
                tags: [ReadinessTags.Ready])
            .AddCheck<SchemaFilesHealthCheck>("schema_files", tags: [ReadinessTags.Ready])
            .AddCheck<ComplianceRulePackHealthCheck>("compliance_rule_pack", tags: [ReadinessTags.Ready])
            .AddCheck<ProcessTempDirectoryHealthCheck>("temp_directory", tags: [ReadinessTags.Ready])
            .AddCheck<BlobStorageHealthCheck>("blob_storage", tags: [ReadinessTags.Ready])
            .AddCheck<CircuitBreakerHealthCheck>(
                "circuit_breakers",
                failureStatus: HealthStatus.Degraded,
                tags: []);

        if (hostingRole is ArchLucidHostingRole.Combined or ArchLucidHostingRole.Worker)
        {
            builder.AddCheck<DataArchivalHostHealthCheck>(
                "data_archival",
                failureStatus: HealthStatus.Degraded,
                tags: [ReadinessTags.Ready]);
        }
    }

    private static void RegisterBackgroundJobs(
        IServiceCollection services,
        IConfiguration configuration,
        ArchLucidHostingRole hostingRole)
    {
        services.Configure<BackgroundJobsOptions>(configuration.GetSection(BackgroundJobsOptions.SectionName));

        BackgroundJobsOptions jobsSnapshot =
            configuration.GetSection(BackgroundJobsOptions.SectionName).Get<BackgroundJobsOptions>() ??
            new BackgroundJobsOptions();

        bool durable = string.Equals(jobsSnapshot.Mode, "Durable", StringComparison.OrdinalIgnoreCase);

        services.AddScoped<IBackgroundJobWorkUnitExecutor, BackgroundJobWorkUnitExecutor>();

        if (hostingRole == ArchLucidHostingRole.Worker)
        {
            if (!durable)
                return;

            RegisterDurableBackgroundJobInfrastructure(services);
            services.AddHostedService<BackgroundJobQueueProcessorHostedService>();

            return;
        }

        if (hostingRole is not (ArchLucidHostingRole.Api or ArchLucidHostingRole.Combined))
        {
            return;
        }

        if (durable)
        {
            RegisterDurableBackgroundJobInfrastructure(services);
            services.AddSingleton<IBackgroundJobQueueNotifySender, AzureStorageQueueBackgroundJobNotifySender>();
            services.AddSingleton<IBackgroundJobQueue, DurableBackgroundJobQueue>();
        }
        else
        {
            services.AddSingleton<IBackgroundJobQueue, InMemoryBackgroundJobQueue>();

            services.AddHostedService(static sp => (InMemoryBackgroundJobQueue)sp.GetRequiredService<IBackgroundJobQueue>());
        }
    }

    private static void RegisterDurableBackgroundJobInfrastructure(IServiceCollection services)
    {
        services.AddScoped<IBackgroundJobRepository, BackgroundJobRepository>();
        services.AddSingleton<IBackgroundJobResultBlobAccessor, AzureBlobBackgroundJobResultBlobAccessor>();
        services.AddSingleton(static sp => CreateBackgroundJobsQueueClient(sp));
    }

    private static QueueClient CreateBackgroundJobsQueueClient(IServiceProvider serviceProvider)
    {
        BackgroundJobsOptions jobsOptions =
            serviceProvider.GetRequiredService<IOptions<BackgroundJobsOptions>>().Value;

        ArtifactLargePayloadOptions? largePayload =
            serviceProvider.GetService<IOptions<ArtifactLargePayloadOptions>>()?.Value;

        TokenCredential credential = serviceProvider.GetRequiredService<TokenCredential>();
        Uri? queueUri = BackgroundJobQueueAddress.ResolveQueueServiceUri(jobsOptions, largePayload);

        if (queueUri is null)
        {
            throw new InvalidOperationException(
                "BackgroundJobs:QueueServiceUri is missing and could not be derived from ArtifactLargePayload:AzureBlobServiceUri. Configure a queue endpoint for durable jobs.");
        }

        if (string.IsNullOrWhiteSpace(jobsOptions.QueueName))
        {
            throw new InvalidOperationException("BackgroundJobs:QueueName is required when BackgroundJobs:Mode is Durable.");
        }

        QueueServiceClient serviceClient = new(queueUri, credential);

        return serviceClient.GetQueueClient(jobsOptions.QueueName);
    }
}
