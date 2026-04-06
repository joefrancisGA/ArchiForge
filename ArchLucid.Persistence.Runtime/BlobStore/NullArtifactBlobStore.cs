namespace ArchiForge.Persistence.BlobStore;

/// <summary>Used when large-payload offload is disabled; callers must not invoke <see cref="WriteAsync"/>.</summary>
public sealed class NullArtifactBlobStore : IArtifactBlobStore
{
    public Task<string> WriteAsync(string containerName, string blobName, string content, CancellationToken ct)
    {
        throw new InvalidOperationException(
            "Artifact blob offload is disabled (ArtifactLargePayload:BlobProvider=None). Configure Local or AzureBlob to write blobs.");
    }

    public Task<string?> ReadAsync(string blobUri, CancellationToken ct) =>
        Task.FromResult<string?>(null);
}
