using System.Text.Json;

using ArchiForge.Core.Diagnostics;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArchiForge.Host.Core.Health;

/// <summary>
/// Writes a richer JSON payload for <c>/health/ready</c> and <c>/health</c> that includes
/// per-check duration, description, error message, and the running build version.
/// <c>/health/live</c> should remain minimal for orchestrator probes.
/// </summary>
public static class DetailedHealthCheckResponseWriter
{
    private static readonly BuildProvenance Provenance =
        BuildProvenance.FromAssembly(typeof(DetailedHealthCheckResponseWriter).Assembly);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(report);

        context.Response.ContentType = "application/json; charset=utf-8";

        // "version" matches GET /version "informationalVersion" (same BuildProvenance source).
        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            version = Provenance.InformationalVersion,
            commitSha = Provenance.CommitSha ?? "(not stamped)",
            entries = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                durationMs = entry.Value.Duration.TotalMilliseconds,
                description = entry.Value.Description,
                error = entry.Value.Exception?.Message,
            }),
        };

        return context.Response.WriteAsJsonAsync(payload, JsonOptions, context.RequestAborted);
    }
}
