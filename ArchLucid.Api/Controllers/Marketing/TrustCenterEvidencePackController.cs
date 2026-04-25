using ArchLucid.Application.Marketing;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;

namespace ArchLucid.Api.Controllers.Marketing;

/// <summary>
///     Anonymous Trust Center evidence-pack endpoint — bundles in-repo procurement
///     artefacts (DPA template, subprocessors, SLA summary, security.txt, CAIQ Lite,
///     SIG Core, owner security self-assessment, pen-test SoW, audit coverage matrix)
///     into a single ZIP with content-driven SHA-256 ETag and a 1-hour response cache.
/// </summary>
/// <remarks>
///     <para>
///         Procurement teams can fetch one URL instead of clicking ten GitHub links;
///         the same content as <c>docs/trust-center.md</c> in a single artefact.
///     </para>
///     <para>
///         <b>Caching strategy.</b> The built ZIP is cached in-process via
///         <see cref="IMemoryCache" /> for one hour keyed by <see cref="CacheKey" />.
///         Subsequent requests reuse the same artefact (and the same ETag) within that
///         window. Clients sending <c>If-None-Match</c> matching the cached ETag receive
///         <c>304 Not Modified</c> with no body. After the window expires the next
///         request triggers a rebuild from the embedded source resources; if source
///         content has not changed the new ETag will match the previous one and
///         downstream caches stay warm.
///     </para>
/// </remarks>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/marketing/trust-center")]
[EnableRateLimiting("fixed")]
[AllowAnonymous]
public sealed class TrustCenterEvidencePackController(
    IEvidencePackBuilder evidencePackBuilder,
    IMemoryCache memoryCache) : ControllerBase
{
    /// <summary>Cache key used by the in-process memory cache for the built artifact.</summary>
    public const string CacheKey = "trust-center-evidence-pack-v1";

    /// <summary>How long a built artifact is reused before the next request triggers a rebuild.</summary>
    public static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(1);

    /// <summary>File name buyers see in their download dialog.</summary>
    public const string DownloadFileName = "archlucid-trust-center-evidence-pack.zip";

    private readonly IEvidencePackBuilder _evidencePackBuilder =
        evidencePackBuilder ?? throw new ArgumentNullException(nameof(evidencePackBuilder));

    private readonly IMemoryCache _memoryCache =
        memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

    /// <summary>Returns the evidence-pack ZIP. Anonymous; cached 1 hour with content-driven ETag.</summary>
    [HttpGet("evidence-pack.zip")]
    [Produces("application/zip")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<IActionResult> GetEvidencePack(CancellationToken cancellationToken = default)
    {
        EvidencePackArtifact artifact = await GetOrBuildAsync(cancellationToken);

        if (TryMatchIfNoneMatch(Request.Headers.IfNoneMatch, artifact.ETag))
        {
            ApplyCachingHeaders(artifact.ETag);
            return StatusCode(StatusCodes.Status304NotModified);
        }

        ApplyCachingHeaders(artifact.ETag);
        return File(artifact.Bytes, artifact.ContentType, DownloadFileName);
    }

    private async Task<EvidencePackArtifact> GetOrBuildAsync(CancellationToken cancellationToken)
    {
        if (_memoryCache.TryGetValue(CacheKey, out EvidencePackArtifact? cached) && cached is not null)
            return cached;

        EvidencePackArtifact built = await _evidencePackBuilder.BuildAsync(cancellationToken);

        MemoryCacheEntryOptions options = new()
        {
            AbsoluteExpirationRelativeToNow = CacheLifetime,
            Size = built.Bytes.LongLength,
        };

        _memoryCache.Set(CacheKey, built, options);
        return built;
    }

    private void ApplyCachingHeaders(string etag)
    {
        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = $"public, max-age={(int)CacheLifetime.TotalSeconds}";
    }

    private static bool TryMatchIfNoneMatch(IList<string?>? ifNoneMatchValues, string artifactEtag)
    {
        if (ifNoneMatchValues is null || ifNoneMatchValues.Count == 0)
            return false;

        foreach (string? raw in ifNoneMatchValues)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            if (raw == "*") return true;

            foreach (string token in raw.Split(','))
            {
                string trimmed = token.Trim();

                if (trimmed.StartsWith("W/", StringComparison.Ordinal))
                    trimmed = trimmed[2..].Trim();

                if (string.Equals(trimmed, artifactEtag, StringComparison.Ordinal))
                    return true;
            }
        }

        return false;
    }
}
