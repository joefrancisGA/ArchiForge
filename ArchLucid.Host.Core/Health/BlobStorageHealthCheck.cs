using ArchLucid.Persistence.BlobStore;

using Azure.Storage.Blobs;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Core.Health;

/// <summary>
/// When <see cref="ArtifactLargePayloadOptions"/> uses Azure Blob, probes the storage account via
/// <see cref="BlobServiceClient.GetPropertiesAsync"/>. Otherwise reports healthy (degraded scope).
/// </summary>
public sealed class BlobStorageHealthCheck(
    IOptionsMonitor<ArtifactLargePayloadOptions> payloadOptions,
    IServiceProvider services) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArtifactLargePayloadOptions o = payloadOptions.CurrentValue;

        if (!o.Enabled || !string.Equals(o.BlobProvider, "AzureBlob", StringComparison.OrdinalIgnoreCase))

            return HealthCheckResult.Healthy(
                "Large artifact offload is not enabled for Azure Blob (readiness scope not applicable).");

        BlobServiceClient? client = services.GetService(typeof(BlobServiceClient)) as BlobServiceClient;

        if (client is null)

            return HealthCheckResult.Unhealthy(
                "ArtifactLargePayload:BlobProvider is AzureBlob but BlobServiceClient is not registered.");

        try
        {
            await client.GetPropertiesAsync(cancellationToken);
            return HealthCheckResult.Healthy("Azure Blob service endpoint responded.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Azure Blob service probe failed.", ex);
        }
    }
}
