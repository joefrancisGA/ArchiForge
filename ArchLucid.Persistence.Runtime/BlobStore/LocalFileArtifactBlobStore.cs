namespace ArchiForge.Persistence.BlobStore;

/// <summary>Writes under a root directory (local dev / tests). URIs use the file:// scheme.</summary>
public sealed class LocalFileArtifactBlobStore : IArtifactBlobStore
{
    private readonly string _rootPath;

    public LocalFileArtifactBlobStore(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        _rootPath = Path.GetFullPath(rootPath);
    }

    public async Task<string> WriteAsync(string containerName, string blobName, string content, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        string safeContainer = SanitizeSegment(containerName);
        string safeName = SanitizeBlobName(blobName);
        string dir = Path.Combine(_rootPath, safeContainer);
        Directory.CreateDirectory(dir);
        string fullPath = Path.Combine(dir, safeName);
        string directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(fullPath, content, ct);
        return new Uri(fullPath).AbsoluteUri;
    }

    public async Task<string?> ReadAsync(string blobUri, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(blobUri))
            return null;

        string path = ToLocalPath(blobUri);
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return null;

        return await File.ReadAllTextAsync(path, ct);
    }

    private static string ToLocalPath(string blobUri)
    {
        try
        {
            Uri uri = new(blobUri, UriKind.Absolute);

            if (uri.IsFile)
                return uri.LocalPath;

            if (Path.IsPathRooted(blobUri))
                return Path.GetFullPath(blobUri);

            return string.Empty;
        }
        catch (UriFormatException)
        {
            return Path.IsPathRooted(blobUri) ? Path.GetFullPath(blobUri) : string.Empty;
        }
    }

    private static string SanitizeSegment(string segment)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            segment = segment.Replace(c, '_');

        return segment.Trim();
    }

    private static string SanitizeBlobName(string blobName)
    {
        blobName = blobName.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

        List<string> parts = new();

        foreach (string part in blobName.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            string s = SanitizeSegment(part);

            if (s.Length > 0)
                parts.Add(s);
        }

        return parts.Count > 0 ? Path.Combine(parts.ToArray()) : "payload.bin";
    }
}
