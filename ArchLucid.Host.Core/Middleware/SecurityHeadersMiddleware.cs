namespace ArchLucid.Host.Core.Middleware;

/// <summary>
/// Adds baseline security headers for API responses (defense in depth; does not replace WAF or browser CSP for SPAs).
/// Production hosts also enable <c>UseHsts()</c> in the pipeline (see <c>PipelineExtensions</c>) for HTTPS clients.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    /// <summary>Content-Security-Policy for JSON API responses (single source of truth for middleware and tests).</summary>
    public const string ContentSecurityPolicyApiJson =
        "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";

    /// <summary>
    /// Anonymous crawler hints (API <c>MapGet</c> for <c>/</c>, <c>/robots.txt</c>, <c>/sitemap.xml</c>) are safe to cache briefly;
    /// <c>no-store</c> on those URLs triggers ZAP passive <c>10049-1</c> (non-storable) without security benefit.
    /// </summary>
    internal static bool IsPublicCrawlerHintPath(PathString path)
    {
        return path == "/" || path == "/robots.txt" || path == "/sitemap.xml";
    }

    public Task InvokeAsync(HttpContext context)
    {
        HttpResponse response = context.Response;
        response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
        response.Headers.TryAdd("X-Frame-Options", "DENY");
        response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
        // API JSON responses: deny active content; tighten further at the edge (Front Door / WAF) for SPAs.
        response.Headers.TryAdd("Content-Security-Policy", ContentSecurityPolicyApiJson);
        // Passive-scan hygiene (ZAP 10015): default API responses are not browser cache assets.
        if (IsPublicCrawlerHintPath(context.Request.Path))
        {
            response.Headers.TryAdd("Cache-Control", "public, max-age=3600");
        }
        else
        {
            response.Headers.TryAdd("Cache-Control", "no-store, max-age=0");
            response.Headers.TryAdd("Pragma", "no-cache");
        }

        // ZAP 10063 Feature-Policy / Permissions-Policy: headless JSON API has no device features.
        response.Headers.TryAdd(
            "Permissions-Policy",
            "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");
        // Declares cross-origin embedding/read posture (ZAP 90004-1).
        response.Headers.TryAdd("Cross-Origin-Resource-Policy", "cross-origin");
        // Site isolation posture expected by ZAP 90004-2; subresources must carry CORP/CORS when embedded cross-origin.
        response.Headers.TryAdd("Cross-Origin-Embedder-Policy", "require-corp");
        // Top-level / navigated-context isolation (ZAP 90004-3); JSON API responses are not framed as cross-origin opener targets.
        response.Headers.TryAdd("Cross-Origin-Opener-Policy", "same-origin");

        return next(context);
    }
}
