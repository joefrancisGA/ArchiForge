using ArchLucid.Core.Scoping;

using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ArchLucid.Persistence.BlobStore;

/// <summary>Azure Blob Storage using a shared <see cref="BlobServiceClient"/> and <see cref="TokenCredential"/>.</summary>
public sealed class AzureBlobArtifactBlobStore(
    BlobServiceClient serviceClient,
    TokenCredential credential,
    IScopeContextProvider scopeProvider) : IArtifactBlobStore
{
    private readonly BlobServiceClient _serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));
    private readonly TokenCredential _credential = credential ?? throw new ArgumentNullException(nameof(credential));
    private readonly IScopeContextProvider _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));

    public async Task<string> WriteAsync(string containerName, string blobName, string content, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        string scopedBlobName = ArtifactBlobTenantPaths.PrefixWithTenant(_scopeProvider, blobName);
        BlobContainerClient container = _serviceClient.GetBlobContainerClient(containerName.ToLowerInvariant());
        await container.CreateIfNotExistsAsync(cancellationToken: ct);
        BlobClient blob = container.GetBlobClient(scopedBlobName);
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
        ArtifactBlobTenantPaths.EnsureReadBlobNameMatchesTenant(_scopeProvider, blob.Name);

        Azure.Response<BlobDownloadResult> response = await blob.DownloadContentAsync(ct);
        return response.Value.Content.ToString();
    }
}
