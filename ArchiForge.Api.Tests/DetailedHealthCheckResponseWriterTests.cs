using System.Text.Json;

using ArchiForge.Api.Health;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class DetailedHealthCheckResponseWriterTests
{
    [Fact]
    public async Task WriteAsync_includes_version_and_entries()
    {
        Dictionary<string, HealthReportEntry> entries = new()
        {
            ["liveness"] = new HealthReportEntry(
                HealthStatus.Healthy,
                "Process is running.",
                TimeSpan.FromMilliseconds(1),
                exception: null,
                data: null),
            ["database"] = new HealthReportEntry(
                HealthStatus.Unhealthy,
                "Cannot connect to SQL.",
                TimeSpan.FromMilliseconds(250),
                exception: new InvalidOperationException("Connection refused"),
                data: null),
        };

        HealthReport report = new(entries, TimeSpan.FromMilliseconds(260));

        DefaultHttpContext httpContext = new();
        httpContext.Response.Body = new MemoryStream();

        await DetailedHealthCheckResponseWriter.WriteAsync(httpContext, report);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using JsonDocument doc = await JsonDocument.ParseAsync(httpContext.Response.Body);
        JsonElement root = doc.RootElement;

        root.GetProperty("status").GetString().Should().Be("Unhealthy");
        root.GetProperty("totalDurationMs").GetDouble().Should().BeGreaterOrEqualTo(0);
        root.TryGetProperty("version", out JsonElement version).Should().BeTrue();
        version.GetString().Should().NotBeNullOrWhiteSpace();
        root.TryGetProperty("commitSha", out _).Should().BeTrue();

        JsonElement entriesArray = root.GetProperty("entries");
        entriesArray.GetArrayLength().Should().Be(2);

        httpContext.Response.ContentType.Should().Contain("application/json");
    }

    [Fact]
    public async Task WriteAsync_healthy_report_returns_healthy_status()
    {
        Dictionary<string, HealthReportEntry> entries = new()
        {
            ["liveness"] = new HealthReportEntry(
                HealthStatus.Healthy,
                "OK",
                TimeSpan.FromMilliseconds(1),
                exception: null,
                data: null),
        };

        HealthReport report = new(entries, TimeSpan.FromMilliseconds(2));

        DefaultHttpContext httpContext = new();
        httpContext.Response.Body = new MemoryStream();

        await DetailedHealthCheckResponseWriter.WriteAsync(httpContext, report);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using JsonDocument doc = await JsonDocument.ParseAsync(httpContext.Response.Body);

        doc.RootElement.GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Fact]
    public async Task WriteAsync_unhealthy_entry_includes_operator_triage_fields()
    {
        Dictionary<string, HealthReportEntry> entries = new()
        {
            ["database"] = new HealthReportEntry(
                HealthStatus.Unhealthy,
                "SQL probe failed.",
                TimeSpan.FromMilliseconds(12),
                exception: new InvalidOperationException("timeout"),
                data: null),
        };

        HealthReport report = new(entries, TimeSpan.FromMilliseconds(15));
        DefaultHttpContext httpContext = new();
        httpContext.Response.Body = new MemoryStream();

        await DetailedHealthCheckResponseWriter.WriteAsync(httpContext, report);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using JsonDocument doc = await JsonDocument.ParseAsync(httpContext.Response.Body);
        JsonElement first = doc.RootElement.GetProperty("entries")[0];

        first.GetProperty("name").GetString().Should().Be("database");
        first.GetProperty("status").GetString().Should().Be("Unhealthy");
        first.GetProperty("description").GetString().Should().Contain("SQL");
        first.GetProperty("error").GetString().Should().Contain("timeout");
        first.TryGetProperty("durationMs", out _).Should().BeTrue();
    }
}
