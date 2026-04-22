using System.Collections.Concurrent;

namespace ArchLucid.Persistence.BlobStore;

/// <summary>In-memory store for deterministic tests; keys are the returned URI string.</summary>
public sealed class InMemoryArtifactBlobStore : IArtifactBlobStore
{
    private readonly ConcurrentDictionary<string, string> _blobs = new(StringComparer.Ordinal);

    public Task<string> WriteAsync(string containerName, string blobName, string content, CancellationToken ct)
    {
        string key = $"memory://{containerName}/{blobName}";
        _blobs[key] = content;
        return Task.FromResult(key);
    }

    public Task<string?> ReadAsync(string blobUri, CancellationToken ct)
    {
        return string.IsNullOrWhiteSpace(blobUri) ? Task.FromResult<string?>(null) : Task.FromResult(_blobs.GetValueOrDefault(blobUri));
    }
}
