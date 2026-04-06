using ArchiForge.Host.Core.Configuration;
using ArchiForge.Application.Jobs;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Options;

namespace ArchiForge.Host.Core.Jobs;

public sealed class AzureBlobBackgroundJobResultBlobAccessor(
    BlobServiceClient blobServiceClient,
    IOptions<BackgroundJobsOptions> options) : IBackgroundJobResultBlobAccessor
{
    public async Task<string> UploadAsync(string jobId, BackgroundJobFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (string.IsNullOrWhiteSpace(jobId))
            throw new ArgumentException("Job id is required.", nameof(jobId));

        string containerName = options.Value.ResultsContainerName;

        if (string.IsNullOrWhiteSpace(containerName))
            throw new InvalidOperationException("BackgroundJobs:ResultsContainerName is required for durable job results.");

        string extension = Path.GetExtension(file.FileName);

        if (string.IsNullOrWhiteSpace(extension))
            extension = ".bin";

        string blobName = $"{jobId}/result{extension}";

        BlobContainerClient container = blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        BlobClient blob = container.GetBlobClient(blobName);

        await blob.UploadAsync(
            new BinaryData(file.Bytes),
            overwrite: true,
            cancellationToken: cancellationToken);

        return blobName;
    }

    public async Task<BackgroundJobFile?> DownloadAsync(
        string blobName,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(blobName))
            return null;

        string containerName = options.Value.ResultsContainerName;

        if (string.IsNullOrWhiteSpace(containerName))
            return null;

        BlobContainerClient container = blobServiceClient.GetBlobContainerClient(containerName);
        BlobClient blob = container.GetBlobClient(blobName);

        if (!await blob.ExistsAsync(cancellationToken))
            return null;

        Response<BlobDownloadResult> response = await blob.DownloadContentAsync(cancellationToken);

        return new BackgroundJobFile(fileName, contentType, response.Value.Content.ToArray());
    }
}
