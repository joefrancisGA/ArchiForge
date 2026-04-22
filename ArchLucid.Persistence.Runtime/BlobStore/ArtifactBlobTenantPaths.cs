using ArchLucid.Core.Scoping;

namespace ArchLucid.Persistence.BlobStore;

/// <summary>
/// Builds tenant-scoped blob paths so shared containers cannot accidentally address another tenant's objects.
/// </summary>
public static class ArtifactBlobTenantPaths
{
    /// <summary>Folder prefix under each container: <c>{tenantId:D}/</c> (GUID with hyphens).</summary>
    public static string TenantPrefixDirectorySegment(Guid tenantId) => tenantId.ToString("D") + "/";

    /// <summary>Rejects path traversal and absolute-style blob names before they reach storage.</summary>
    public static void ThrowIfBlobRelativePathUnsafe(string blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name is required.", nameof(blobName));

        if (blobName.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException("Blob paths containing '..' are not allowed.");

        if (blobName.StartsWith("/", StringComparison.Ordinal) || blobName.StartsWith("\\", StringComparison.Ordinal))
            throw new InvalidOperationException("Blob paths must be relative (no leading slash).");
    }

    /// <summary>
    /// Prefixes <paramref name="blobName"/> with the current tenant directory. Callers must pass a logical path
    /// without an embedded tenant prefix (that would double-prefix or confuse audits).
    /// </summary>
    public static string PrefixWithTenant(IScopeContextProvider scopeProvider, string blobName)
    {
        if (scopeProvider is null)
            throw new ArgumentNullException(nameof(scopeProvider));

        ThrowIfBlobRelativePathUnsafe(blobName);
        Guid tenantId = scopeProvider.GetCurrentScope().TenantId;
        string prefix = TenantPrefixDirectorySegment(tenantId);
        string normalized = blobName.Replace("\\", "/", StringComparison.Ordinal).TrimStart('/');
        string[] topSegments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (topSegments.Length <= 0 || !Guid.TryParse(topSegments[0], out Guid leadingFolder))
            return prefix + normalized;

        if (leadingFolder == tenantId)
            throw new InvalidOperationException("Blob name must not include a tenant prefix; it is applied automatically.");

        throw new InvalidOperationException("Blob name must not start with another tenant folder segment.");

    }

    /// <summary>
    /// Ensures a blob name returned from storage (e.g. <see cref="Azure.Storage.Blobs.BlobClient.Name"/>) belongs to
    /// the tenant resolved from <paramref name="scopeProvider"/>.
    /// </summary>
    public static void EnsureReadBlobNameMatchesTenant(IScopeContextProvider scopeProvider, string blobName)
    {
        if (scopeProvider is null)
            throw new ArgumentNullException(nameof(scopeProvider));

        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name is required.", nameof(blobName));

        Guid tenantId = scopeProvider.GetCurrentScope().TenantId;
        string prefix = TenantPrefixDirectorySegment(tenantId);

        if (!blobName.StartsWith(prefix, StringComparison.Ordinal))

            throw new InvalidOperationException(
                "Blob path is outside the current tenant scope; it must start with the tenant folder segment.");


        string remainder = blobName.Length > prefix.Length ? blobName[prefix.Length..] : string.Empty;
        ThrowIfBlobRelativePathUnsafe(remainder);
    }
}
