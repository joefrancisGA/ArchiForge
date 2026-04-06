using System.Collections.Concurrent;

namespace ArchiForge.Persistence.BlobStore;

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
        if (string.IsNullOrWhiteSpace(blobUri))
            return Task.FromResult<string?>(null);

        return Task.FromResult(_blobs.TryGetValue(blobUri, out string? value) ? value : null);
    }
}
