using System.Diagnostics;
using System.Text.RegularExpressions;

using ArchiForge.Core.Diagnostics;

using Serilog.Context;

namespace ArchiForge.Host.Core.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-ID";
    private const int MaxCorrelationIdLength = 64;

    // Only allow safe characters: alphanumeric, hyphens, underscores, and dots.
    private static readonly Regex SafeCorrelationIdPattern =
        new(@"^[a-zA-Z0-9\-_.]+$", RegexOptions.Compiled);

    public async Task InvokeAsync(HttpContext context)
    {
        string? rawHeader = context.Request.Headers[HeaderName].FirstOrDefault();
        string correlationId = IsValidCorrelationId(rawHeader)
            ? rawHeader!
            : context.TraceIdentifier;

        context.Response.Headers[HeaderName] = correlationId;
        context.TraceIdentifier = correlationId;

        Activity? activity = Activity.Current;
        if (activity is not null)
        {
            activity.SetTag(ActivityCorrelation.LogicalCorrelationIdTag, correlationId);
            activity.SetTag("http.request_id", context.TraceIdentifier);
            string? runId = context.Request.RouteValues["runId"]?.ToString();
            if (!string.IsNullOrEmpty(runId))
                activity.SetTag("archiforge.run_id", runId);
        }

        using (LogContext.PushProperty("CorrelationId", correlationId))
        
            await next(context);
        
    }

    private static bool IsValidCorrelationId(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= MaxCorrelationIdLength
        && SafeCorrelationIdPattern.IsMatch(value);
}
