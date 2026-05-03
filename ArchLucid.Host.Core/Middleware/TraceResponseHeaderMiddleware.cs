using System.Diagnostics;

namespace ArchLucid.Host.Core.Middleware;

/// <summary>
/// Adds W3C <c>traceparent</c> and a plain <c>X-Trace-Id</c> on every response so browsers and operators can correlate with distributed traces.
/// </summary>
public sealed class TraceResponseHeaderMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // OnStarting runs immediately before headers are sent so Activity.Current reflects the final request span.
        context.Response.OnStarting(() =>
        {
            Activity? activity = Activity.Current;

            if (activity is null)
                return Task.CompletedTask;

            context.Response.Headers["traceparent"] = activity.Id;
            context.Response.Headers["X-Trace-Id"] = activity.TraceId.ToString();

            return Task.CompletedTask;
        });

        return _next(context);
    }
}
