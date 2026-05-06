using ArchLucid.Contracts.Ingestion;

namespace ArchLucid.Application.Ingestion;
/// <summary>
///     Builds a minimal C4-shaped preview from a repository URL without cloning or LLM work (time-to-value helper).
/// </summary>
public static class FastPathContextModelBuilder
{
    /// <summary>Throws <see cref = "ArgumentException"/> when <paramref name = "rawUrl"/> is not an absolute http(s) URI.</summary>
    public static FastPathContextPreviewResponse Build(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
            throw new ArgumentException("Repository URL is required.", nameof(rawUrl));
        string trimmed = rawUrl.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            throw new ArgumentException("Repository URL must be an absolute http(s) URI.", nameof(rawUrl));
        }

        string slug = ExtractRepositorySlug(uri);
        string systemTitle = SlugToTitle(slug);
        List<FastPathContextElementDto> elements = [new FastPathContextElementDto
        {
            ElementId = "fast-system-1",
            Name = systemTitle,
            Kind = "SoftwareSystem",
            ReasoningTrace = "Heuristic: software system name taken from the last non-empty path segment of the repository URL."
        }, new FastPathContextElementDto
        {
            ElementId = "fast-container-app",
            Name = $"{systemTitle} workload",
            Kind = "Container",
            ReasoningTrace = "Heuristic placeholder for compute / application tier. Run full context ingest for repository-accurate components."
        }, new FastPathContextElementDto
        {
            ElementId = "fast-container-data",
            Name = $"{systemTitle} data",
            Kind = "Container",
            ReasoningTrace = "Heuristic placeholder for persistence. Refine with connector inventory after deep ingest."
        }

        ];
        string combined = $"{uri.AbsoluteUri} {slug}".ToUpperInvariant();
        if (combined.Contains("API", StringComparison.Ordinal))
        {
            elements.Add(new FastPathContextElementDto { ElementId = "fast-container-api", Name = "Public HTTP API", Kind = "Container", ReasoningTrace = "Heuristic: 'api' token detected in URL or slug — likely HTTP boundary." });
        }

        if (combined.Contains("UI", StringComparison.Ordinal) || combined.Contains("WEB", StringComparison.Ordinal) || combined.Contains("FRONT", StringComparison.Ordinal))
        {
            elements.Add(new FastPathContextElementDto { ElementId = "fast-container-web", Name = "Web client", Kind = "Container", ReasoningTrace = "Heuristic: UI/web/front token detected in URL or slug — likely browser or SPA surface." });
        }

        return new FastPathContextPreviewResponse
        {
            SourceUrl = trimmed,
            Elements = elements,
            Mode = "heuristic-v1"
        };
    }

    private static string ExtractRepositorySlug(Uri uri)
    {
        string path = uri.AbsolutePath.Trim('/');
        if (path.Length == 0)
            return uri.Host;
        ReadOnlySpan<char> span = path.AsSpan();
        int slash = span.LastIndexOf('/');
        ReadOnlySpan<char> last = slash >= 0 ? span[(slash + 1)..] : span;
        if (last.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            last = last[..^4];
        string name = last.ToString();
        return name.Length > 0 ? name : uri.Host;
    }

    private static string SlugToTitle(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return "Target system";
        string[] parts = slug.Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 0)
                continue;
            string p = parts[i];
            parts[i] = char.ToUpperInvariant(p[0]) + (p.Length > 1 ? p[1..].ToLowerInvariant() : "");
        }

        return string.Join(' ', parts);
    }
}