using ArchLucid.Host.Core.Configuration;
using ArchLucid.Persistence.BlobStore;

namespace ArchLucid.Host.Core.Startup.Validation.Rules;

internal static class BackgroundJobsRules
{
    public static void Collect(IConfiguration configuration, List<string> errors)
    {
        BackgroundJobsOptions jobs =
            configuration.GetSection(BackgroundJobsOptions.SectionName).Get<BackgroundJobsOptions>() ??
            new BackgroundJobsOptions();

        if (!string.Equals(jobs.Mode, "Durable", StringComparison.OrdinalIgnoreCase))
            return;

        ArchLucidOptions archi = ArchLucidConfigurationBridge.ResolveArchLucidOptions(configuration);

        if (!ArchLucidOptions.EffectiveIsSql(archi.StorageProvider))

            errors.Add("BackgroundJobs:Mode Durable requires ArchLucid:StorageProvider Sql (shared job state in SQL).");

        ArtifactLargePayloadOptions large =
            configuration.GetSection(ArtifactLargePayloadOptions.SectionName).Get<ArtifactLargePayloadOptions>() ??
            new ArtifactLargePayloadOptions();

        if (!string.Equals(large.BlobProvider, "AzureBlob", StringComparison.OrdinalIgnoreCase))

            errors.Add(
                "BackgroundJobs:Mode Durable requires ArtifactLargePayload:BlobProvider AzureBlob (queue + result blobs via managed identity).");

        if (string.IsNullOrWhiteSpace(large.AzureBlobServiceUri) && string.IsNullOrWhiteSpace(jobs.QueueServiceUri))

            errors.Add(
                "BackgroundJobs:Mode Durable requires BackgroundJobs:QueueServiceUri or ArtifactLargePayload:AzureBlobServiceUri.");

        if (string.IsNullOrWhiteSpace(jobs.ResultsContainerName))

            errors.Add("BackgroundJobs:ResultsContainerName must be set when BackgroundJobs:Mode is Durable.");

        int receiveBatch = configuration.GetValue("BackgroundJobs:ProcessorReceiveBatchSize", 16);

        if (receiveBatch is < 1 or > 32)

            errors.Add(
                "BackgroundJobs:ProcessorReceiveBatchSize must be between 1 and 32 when BackgroundJobs:Mode is Durable.");
    }
}
