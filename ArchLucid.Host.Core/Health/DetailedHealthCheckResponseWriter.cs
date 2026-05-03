using System.Text.Json;

using ArchLucid.Core.Diagnostics;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArchLucid.Host.Core.Health;

/// <summary>
/// Writes JSON for health endpoints. Use <see cref="HealthCheckResponseDetailLevel.Summary"/> for anonymous
/// readiness probes; use <see cref="HealthCheckResponseDetailLevel.Detailed"/> only behind authentication.
/// <c>/health/live</c> should remain minimal for orchestrator probes (default writer).
/// </summary>
public static class DetailedHealthCheckResponseWriter
{
    private static readonly BuildProvenance Provenance =
        BuildProvenance.FromAssembly(typeof(DetailedHealthCheckResponseWriter).Assembly);

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, };

    /// <summary>Writes detailed JSON (backward-compatible default for callers that omit the level).</summary>
    public static Task WriteAsync(HttpContext context, HealthReport report) =>
        WriteAsync(context, report, HealthCheckResponseDetailLevel.Detailed);

    public static Task WriteAsync(
        HttpContext context,
        HealthReport report,
        HealthCheckResponseDetailLevel detailLevel)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(report);

        context.Response.ContentType = "application/json; charset=utf-8";

        return detailLevel switch
        {
            HealthCheckResponseDetailLevel.Summary => WriteSummaryPayloadAsync(context, report),
            HealthCheckResponseDetailLevel.Detailed => WriteDetailedPayloadAsync(context, report),
            _ => throw new ArgumentOutOfRangeException(nameof(detailLevel), detailLevel, null),
        };
    }

    private static Task WriteSummaryPayloadAsync(HttpContext context, HealthReport report)
    {
        var payload = new
        {
            status = report.Status.ToString(), entries = report.Entries.Select(entry => new { name = entry.Key, status = entry.Value.Status.ToString(), }),
        };

        return context.Response.WriteAsJsonAsync(payload, JsonOptions, context.RequestAborted);
    }

    private static Task WriteDetailedPayloadAsync(HttpContext context, HealthReport report)
    {
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
                data = entry.Value.Data is { Count: > 0 } ? entry.Value.Data : null,
            }),
        };

        return context.Response.WriteAsJsonAsync(payload, JsonOptions, context.RequestAborted);
    }
}
