using System.Security.Cryptography;
using System.Text.Json;

using ArchLucid.Api.Attributes;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Api.Serialization;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Configuration;
using ArchLucid.Host.Core.Demo;
using ArchLucid.Persistence.Caching;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace ArchLucid.Api.Controllers.Demo;

/// <summary>
///     Public, read-only marketing surface: one JSON bundle shaped like the operator commit page for the latest committed
///     demo-seed run.
/// </summary>
/// <remarks>
///     <para>
///         <b>Cache key:</b> stable <c>demo-preview:bundle:v1:latest</c>. The resolved
///         <see cref="DemoCommitPagePreviewResponse.Run" /> id and
///         manifest identity live inside the cached value, so a re-seed that produces a new run replaces the payload on
///         the next cache miss
///         without a distributed invalidation hook (TTL-only staleness, up to
///         <see cref="DemoOptions.PreviewCacheSeconds" />).
///     </para>
/// </remarks>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/demo")]
[EnableRateLimiting("fixed")]
[AllowAnonymous]
[FeatureGate(FeatureGateKey.DemoEnabled)]
public sealed class DemoCommitPagePreviewController(
    IDemoCommitPagePreviewClient previewClient,
    IHotPathReadCache hotPathReadCache,
    IOptionsMonitor<DemoOptions> demoOptions) : ControllerBase
{
    /// <summary>
    ///     Stable v1 bundle key. Run id + manifest version are carried in the cached
    ///     <see cref="DemoCommitPagePreviewResponse" />, not in the key,
    ///     so the entry hot-swaps after TTL on re-seed without explicit eviction.
    /// </summary>
    private const string PreviewBundleCacheKey = "demo-preview:bundle:v1:latest";

    private const string PreviewCacheControl =
        "public, max-age=300, s-maxage=300, stale-while-revalidate=60";

    private readonly IOptionsMonitor<DemoOptions> _demoOptions =
        demoOptions ?? throw new ArgumentNullException(nameof(demoOptions));

    private readonly IHotPathReadCache _hotPathReadCache =
        hotPathReadCache ?? throw new ArgumentNullException(nameof(hotPathReadCache));

    private readonly IDemoCommitPagePreviewClient _previewClient =
        previewClient ?? throw new ArgumentNullException(nameof(previewClient));

    /// <summary>Public procurement alias — identical payload to <c>GET preview</c> (rate limit + demo gate apply).</summary>
    [HttpGet("/v{version:apiVersion}/public/demo/sample-run")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(DemoCommitPagePreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetPublicDemoSampleRun(CancellationToken cancellationToken = default) =>
        GetDemoCommitPagePreview(cancellationToken);

    /// <summary>Returns the bundled commit-page preview JSON for the latest committed demo-seed run.</summary>
    [HttpGet("preview")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(DemoCommitPagePreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDemoCommitPagePreview(CancellationToken cancellationToken = default)
    {
        bool materialized = false;
        int ttlSeconds = ClampPreviewCacheSeconds(_demoOptions.CurrentValue);

        DemoCommitPagePreviewResponse? payload = await _hotPathReadCache.GetOrCreateAsync(
            PreviewBundleCacheKey,
            async ct =>
            {
                materialized = true;

                return await _previewClient.GetLatestCommittedDemoCommitPageAsync(ct);
            },
            cancellationToken,
            null,
            ttlSeconds);

        if (payload is null)
            return this.NotFoundProblem(
                "No committed demo-seed run is available on this host. Run `archlucid try` or POST /v1/demo/seed and retry.",
                ProblemTypes.RunNotFound);

        if (materialized)
            ArchLucidInstrumentation.DemoPreviewCacheMisses.Add(1);
        else
            ArchLucidInstrumentation.DemoPreviewCacheHits.Add(1);

        byte[] body = JsonSerializer.SerializeToUtf8Bytes(payload, ArchLucidApiJsonSerializerOptions.Web);
        string etag = $"\"{Convert.ToHexString(SHA256.HashData(body)).ToLowerInvariant()}\"";

        Response.Headers["Cache-Control"] = PreviewCacheControl;
        Response.Headers["ETag"] = etag;

        if (!Request.Headers.TryGetValue("If-None-Match", out StringValues inm))
            return File(body, "application/json");

        if (inm.OfType<string>().Any(candidate => string.Equals(candidate.Trim(), etag, StringComparison.Ordinal)))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        return File(body, "application/json");
    }

    private static int ClampPreviewCacheSeconds(DemoOptions options)
    {
        int seconds = options.PreviewCacheSeconds;

        if (seconds < 1)
            seconds = 300;

        return Math.Clamp(seconds, 30, 3600);
    }
}
