using ArchiForge.Application.Jobs;
using ArchiForge.Host.Core.Configuration;
using ArchiForge.Host.Core.Health;
using ArchiForge.Host.Core.Hosting;
using ArchiForge.Host.Core.Jobs;
using ArchiForge.Persistence.BlobStore;
using ArchiForge.Persistence.Data.Infrastructure;
using ArchiForge.Persistence.Data.Repositories;

using Azure.Core;
using Azure.Storage.Queues;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ArchiForge.Host.Composition.Startup;

public static partial class ServiceCollectionExtensions
{
    private static void RegisterDataInfrastructure(IServiceCollection services, IConfiguration configuration)
    {
        ArchiForgeOptions mode = ArchiForgeConfigurationBridge.ResolveArchiForgeOptions(configuration);

        if (!string.Equals(mode.StorageProvider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
    }

    private static void RegisterArchiForgeHealthChecks(IServiceCollection services, ArchiForgeHostingRole hostingRole)
    {
        IHealthChecksBuilder builder = services.AddHealthChecks()
            .AddCheck(
                "liveness",
                () => HealthCheckResult.Healthy("ArchiForge API process is running."),
                tags: [ReadinessTags.Live])
            .AddCheck<SqlConnectionHealthCheck>(
                "database",
                failureStatus: HealthStatus.Unhealthy,
                tags: [ReadinessTags.Ready])
            .AddCheck<SchemaFilesHealthCheck>("schema_files", tags: [ReadinessTags.Ready])
            .AddCheck<ComplianceRulePackHealthCheck>("compliance_rule_pack", tags: [ReadinessTags.Ready])
            .AddCheck<ProcessTempDirectoryHealthCheck>("temp_directory", tags: [ReadinessTags.Ready])
            .AddCheck<BlobStorageHealthCheck>("blob_storage", tags: [ReadinessTags.Ready]);

        if (hostingRole is ArchiForgeHostingRole.Combined or ArchiForgeHostingRole.Worker)
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
        ArchiForgeHostingRole hostingRole)
    {
        services.Configure<BackgroundJobsOptions>(configuration.GetSection(BackgroundJobsOptions.SectionName));

        BackgroundJobsOptions jobsSnapshot =
            configuration.GetSection(BackgroundJobsOptions.SectionName).Get<BackgroundJobsOptions>() ??
            new BackgroundJobsOptions();

        bool durable = string.Equals(jobsSnapshot.Mode, "Durable", StringComparison.OrdinalIgnoreCase);

        services.AddScoped<IBackgroundJobWorkUnitExecutor, BackgroundJobWorkUnitExecutor>();

        if (hostingRole == ArchiForgeHostingRole.Worker)
        {
            if (durable)
            {
                RegisterDurableBackgroundJobInfrastructure(services);
                services.AddHostedService<BackgroundJobQueueProcessorHostedService>();
            }

            return;
        }

        if (hostingRole is not (ArchiForgeHostingRole.Api or ArchiForgeHostingRole.Combined))
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
