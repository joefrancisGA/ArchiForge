namespace ArchiForge.Host.Core.Middleware;

/// <summary>
/// Adds baseline security headers for API responses (defense in depth; does not replace WAF or browser CSP for SPAs).
/// Production hosts also enable <c>UseHsts()</c> in the pipeline (see <c>PipelineExtensions</c>) for HTTPS clients.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        HttpResponse response = context.Response;
        response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
        response.Headers.TryAdd("X-Frame-Options", "DENY");
        response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
        // API JSON responses: deny active content; tighten further at the edge (Front Door / WAF) for SPAs.
        response.Headers.TryAdd(
            "Content-Security-Policy",
            "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'");

        return next(context);
    }
}
