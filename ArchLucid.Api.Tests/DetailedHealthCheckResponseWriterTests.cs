using System.Text.Json;

using ArchLucid.Host.Core.Health;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArchLucid.Api.Tests;

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
                null,
                null),
            ["database"] = new HealthReportEntry(
                HealthStatus.Unhealthy,
                "Cannot connect to SQL.",
                TimeSpan.FromMilliseconds(250),
                new InvalidOperationException("Connection refused"),
                null)
        };

        HealthReport report = new(entries, TimeSpan.FromMilliseconds(260));

        DefaultHttpContext httpContext = new()
        {
            Response = { Body = new MemoryStream() }
        };

        await DetailedHealthCheckResponseWriter.WriteAsync(httpContext, report);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using JsonDocument doc = await JsonDocument.ParseAsync(httpContext.Response.Body);
        JsonElement root = doc.RootElement;

        root.GetProperty("status").GetString().Should().Be("Unhealthy");
        root.GetProperty("totalDurationMs").GetDouble().Should().BeGreaterThanOrEqualTo(0);
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
                null,
                null)
        };

        HealthReport report = new(entries, TimeSpan.FromMilliseconds(2));

        DefaultHttpContext httpContext = new()
        {
            Response = { Body = new MemoryStream() }
        };

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
                new InvalidOperationException("timeout"),
                null)
        };

        HealthReport report = new(entries, TimeSpan.FromMilliseconds(15));
        DefaultHttpContext httpContext = new()
        {
            Response = { Body = new MemoryStream() }
        };

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

    [Fact]
    public async Task WriteAsync_detailed_includes_data_when_health_entry_has_dictionary()
    {
        Dictionary<string, object> checkData = new()
        {
            ["gates"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["name"] = "OpenAiCompletion",
                    ["state"] = "Closed",
                    ["consecutiveFailures"] = 0,
                    ["failureThreshold"] = 5,
                    ["breakDurationSeconds"] = 30,
                    ["lastStateChangeUtc"] = "never"
                }
            }
        };

        Dictionary<string, HealthReportEntry> entries = new()
        {
            ["circuit_breakers"] = new HealthReportEntry(
                HealthStatus.Healthy,
                "All OpenAI circuit breakers closed.",
                TimeSpan.FromMilliseconds(2),
                null,
                checkData)
        };

        HealthReport report = new(entries, TimeSpan.FromMilliseconds(5));
        DefaultHttpContext httpContext = new()
        {
            Response = { Body = new MemoryStream() }
        };

        await DetailedHealthCheckResponseWriter.WriteAsync(httpContext, report);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using JsonDocument doc = await JsonDocument.ParseAsync(httpContext.Response.Body);
        JsonElement entry = doc.RootElement.GetProperty("entries")[0];

        entry.TryGetProperty("data", out JsonElement dataEl).Should().BeTrue();
        dataEl.GetProperty("gates").GetArrayLength().Should().Be(1);
        JsonElement gate0 = dataEl.GetProperty("gates")[0];
        gate0.GetProperty("name").GetString().Should().Be("OpenAiCompletion");
        gate0.GetProperty("state").GetString().Should().Be("Closed");
        gate0.GetProperty("consecutiveFailures").GetInt32().Should().Be(0);
        gate0.GetProperty("failureThreshold").GetInt32().Should().Be(5);
        gate0.GetProperty("breakDurationSeconds").GetInt32().Should().Be(30);
        gate0.GetProperty("lastStateChangeUtc").GetString().Should().Be("never");
    }

    [Fact]
    public async Task WriteAsync_detailed_serializes_circuit_breaker_last_transition_roundtrip()
    {
        string lastChange = new DateTimeOffset(2026, 4, 10, 15, 30, 0, TimeSpan.Zero).ToString("o");
        Dictionary<string, object> checkData = new()
        {
            ["gates"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["name"] = "OpenAiEmbedding",
                    ["state"] = "Open",
                    ["consecutiveFailures"] = 5,
                    ["failureThreshold"] = 5,
                    ["breakDurationSeconds"] = 60,
                    ["lastStateChangeUtc"] = lastChange
                }
            }
        };

        Dictionary<string, HealthReportEntry> entries = new()
        {
            ["circuit_breakers"] = new HealthReportEntry(
                HealthStatus.Degraded,
                "One or more OpenAI circuits are open or probing.",
                TimeSpan.FromMilliseconds(3),
                null,
                checkData)
        };

        HealthReport report = new(entries, TimeSpan.FromMilliseconds(6));
        DefaultHttpContext httpContext = new()
        {
            Response = { Body = new MemoryStream() }
        };

        await DetailedHealthCheckResponseWriter.WriteAsync(httpContext, report);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using JsonDocument doc = await JsonDocument.ParseAsync(httpContext.Response.Body);
        JsonElement gate0 = doc.RootElement.GetProperty("entries")[0].GetProperty("data").GetProperty("gates")[0];

        gate0.GetProperty("lastStateChangeUtc").GetString().Should().Be(lastChange);
        gate0.GetProperty("consecutiveFailures").GetInt32().Should().Be(5);
    }

    [Fact]
    public async Task WriteAsync_summary_omits_version_duration_and_error()
    {
        Dictionary<string, HealthReportEntry> entries = new()
        {
            ["database"] = new HealthReportEntry(
                HealthStatus.Unhealthy,
                "SQL probe failed.",
                TimeSpan.FromMilliseconds(12),
                new InvalidOperationException("timeout"),
                null)
        };

        HealthReport report = new(entries, TimeSpan.FromMilliseconds(15));
        DefaultHttpContext httpContext = new()
        {
            Response = { Body = new MemoryStream() }
        };

        await DetailedHealthCheckResponseWriter.WriteAsync(httpContext, report, HealthCheckResponseDetailLevel.Summary);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using JsonDocument doc = await JsonDocument.ParseAsync(httpContext.Response.Body);
        JsonElement root = doc.RootElement;

        root.TryGetProperty("version", out _).Should().BeFalse();
        root.TryGetProperty("commitSha", out _).Should().BeFalse();
        root.TryGetProperty("totalDurationMs", out _).Should().BeFalse();

        JsonElement first = root.GetProperty("entries")[0];
        first.GetProperty("name").GetString().Should().Be("database");
        first.GetProperty("status").GetString().Should().Be("Unhealthy");
        first.TryGetProperty("error", out _).Should().BeFalse();
        first.TryGetProperty("description", out _).Should().BeFalse();
    }
}
