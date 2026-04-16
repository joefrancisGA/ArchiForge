using ArchLucid.Core.Scoping;

namespace ArchLucid.Persistence.BlobStore;

/// <summary>Writes under a root directory (local dev / tests). URIs use the file:// scheme.</summary>
public sealed class LocalFileArtifactBlobStore : IArtifactBlobStore
{
    private readonly string _rootPath;
    private readonly IScopeContextProvider _scopeProvider;

    public LocalFileArtifactBlobStore(string rootPath, IScopeContextProvider scopeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        _rootPath = Path.GetFullPath(rootPath);
        _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
    }

    public async Task<string> WriteAsync(string containerName, string blobName, string content, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        string safeContainer = SanitizeSegment(containerName);
        string scopedLogical = ArtifactBlobTenantPaths.PrefixWithTenant(_scopeProvider, blobName);
        string safeName = SanitizeBlobName(scopedLogical);
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

        string full = Path.GetFullPath(path);
        string root = Path.GetFullPath(_rootPath);

        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            return null;

        string relative = Path.GetRelativePath(root, full);
        string[] segments = relative.Split(
            new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length < 2)
            throw new InvalidOperationException("Local blob path must include container and tenant folder segments.");

        if (!Guid.TryParse(segments[1], out Guid pathTenant) ||
            pathTenant != _scopeProvider.GetCurrentScope().TenantId)
        {
            throw new InvalidOperationException("Local blob path tenant folder does not match the current tenant scope.");
        }

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
