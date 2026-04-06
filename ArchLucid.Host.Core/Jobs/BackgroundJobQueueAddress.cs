using ArchiForge.Host.Core.Configuration;

using ArchiForge.Persistence.BlobStore;

namespace ArchiForge.Host.Core.Jobs;

/// <summary>Resolves the Azure Queue service endpoint for durable jobs.</summary>
public static class BackgroundJobQueueAddress
{
    public static Uri? ResolveQueueServiceUri(BackgroundJobsOptions jobs, ArtifactLargePayloadOptions? largePayload)
    {
        ArgumentNullException.ThrowIfNull(jobs);

        string? direct = jobs.QueueServiceUri?.Trim();

        if (!string.IsNullOrWhiteSpace(direct))
            return new Uri(direct, UriKind.Absolute);

        string? blobUri = largePayload?.AzureBlobServiceUri.Trim();

        if (string.IsNullOrWhiteSpace(blobUri))
            return null;

        if (!blobUri.Contains(".blob.", StringComparison.OrdinalIgnoreCase))
            return null;

        string queueUri = blobUri.Replace(".blob.", ".queue.", StringComparison.OrdinalIgnoreCase);

        return new Uri(queueUri, UriKind.Absolute);
    }
}
