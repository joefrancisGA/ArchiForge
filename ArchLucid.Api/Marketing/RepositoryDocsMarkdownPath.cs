namespace ArchLucid.Api.Marketing;

/// <summary>
/// Resolves Markdown under the repository <c>docs</c> folder by walking up from <see cref="Microsoft.AspNetCore.Hosting.IWebHostEnvironment.ContentRootPath" />.
/// Test hosts and <c>dotnet run</c> disagree on ContentRoot depth; fixed <c>..\..\docs\…</c> chains can 404 in integration tests otherwise.
/// </summary>
internal static class RepositoryDocsMarkdownPath
{
    private const int MaxAncestorsWalk = 10;

    public static string? TryFindFile(IWebHostEnvironment hostEnvironment, params string[] relativeUnderDocs)
    {
        ArgumentNullException.ThrowIfNull(hostEnvironment);
        if (relativeUnderDocs is null || relativeUnderDocs.Length == 0)
            throw new ArgumentException("At least one path segment under docs/ is required.", nameof(relativeUnderDocs));

        string[] relativePathParts =
            relativeUnderDocs.Where(static s => !string.IsNullOrWhiteSpace(s)).ToArray();

        string? cursor = Path.GetFullPath(hostEnvironment.ContentRootPath);

        for (int depth = 0; depth <= MaxAncestorsWalk && cursor is not null; depth++)
        {
            string candidate = Path.Combine(cursor, "docs", Path.Combine(relativePathParts));

            if (File.Exists(candidate))
                return candidate;

            cursor = Directory.GetParent(cursor)?.FullName;
        }

        return null;
    }
}
