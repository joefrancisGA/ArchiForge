using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ArchiForge.Persistence.BlobStore;

/// <summary>Azure Blob Storage using a shared <see cref="BlobServiceClient"/> and <see cref="TokenCredential"/>.</summary>
public sealed class AzureBlobArtifactBlobStore : IArtifactBlobStore
{
    private readonly BlobServiceClient _serviceClient;
    private readonly TokenCredential _credential;

    public AzureBlobArtifactBlobStore(BlobServiceClient serviceClient, TokenCredential credential)
    {
        _serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));
        _credential = credential ?? throw new ArgumentNullException(nameof(credential));
    }

    public async Task<string> WriteAsync(string containerName, string blobName, string content, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        BlobContainerClient container = _serviceClient.GetBlobContainerClient(containerName.ToLowerInvariant());
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
        BlobClient blob = container.GetBlobClient(blobName);
        await blob.UploadAsync(
            new BinaryData(content),
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "application/json; charset=utf-8" },
            },
            cancellationToken: ct);
        return blob.Uri.ToString();
    }

    public async Task<string?> ReadAsync(string blobUri, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(blobUri))
            return null;

        BlobClient blob = new(new Uri(blobUri, UriKind.Absolute), _credential);
        Azure.Response<BlobDownloadResult> response = await blob.DownloadContentAsync(ct);
        return response.Value.Content.ToString();
    }
}
