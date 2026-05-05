using ArchLucid.Api.Middleware;
using ArchLucid.Api.Tests.Http;
using ArchLucid.Host.Core.Middleware;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Unit tests for <see cref="HttpRequestLoggingMiddleware" />.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class HttpRequestLoggingMiddlewareTests
{
    [Fact]
    public void ResolveCorrelationIdentifier_reads_response_header_trimmed()
    {
        DefaultHttpContext context = new();

        context.Response.Headers["X-Correlation-ID"] = "  corr-xyz  ";

        HttpRequestLoggingMiddleware.ResolveCorrelationIdentifierForLogging(context)
            .Should()
            .Be("corr-xyz");
    }

    [Fact]
    public void ResolveCorrelationIdentifier_falls_back_when_header_blank()
    {
        DefaultHttpContext context = new() { TraceIdentifier = "trace-fallback" };

        context.Response.Headers["X-Correlation-ID"] = "    ";

        HttpRequestLoggingMiddleware.ResolveCorrelationIdentifierForLogging(context)
            .Should()
            .Be("trace-fallback");
    }

    [Fact]
    public void ResolveCorrelationIdentifier_returns_null_when_no_signals()
    {
        DefaultHttpContext context = new()
        {
            TraceIdentifier = string.Empty
        };

        HttpRequestLoggingMiddleware.ResolveCorrelationIdentifierForLogging(context)
            .Should()
            .BeNull();
    }

    [Fact]
    public async Task Middleware_logs_start_then_finish_with_timing_and_status()
    {
        RecordingLoggerProvider sink = new();

        using (ILoggerFactory factory = LoggerFactory.Create(b =>
               {
                   b.AddProvider(sink);
                   b.SetMinimumLevel(LogLevel.Information);
               }))
        {
            ILogger<HttpRequestLoggingMiddleware> logger =
                factory.CreateLogger<HttpRequestLoggingMiddleware>();

            RequestDelegate terminator = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status418ImATeapot;

                return Task.CompletedTask;
            };

            HttpRequestLoggingMiddleware sut = new(terminator, logger);

            DefaultHttpContext context = new();

            context.Request.Method = HttpMethods.Get;

            context.Request.Path = "/v1/sample";

            context.TraceIdentifier = "trace-under-test";

            context.Response.Headers["X-Correlation-ID"] = "trace-under-test";

            await sut.InvokeAsync(context);

            IList<(LogLevel Level, EventId EventId, string Message)> entries = sink.Entries;

            entries.Should().HaveCount(2);

            entries.Should()
                .Contain(e =>
                    e.Message.StartsWith("HTTP request started", StringComparison.Ordinal)
                    && e.Message.Contains(HttpMethods.Get, StringComparison.Ordinal)
                    && e.Message.Contains("trace-under-test", StringComparison.Ordinal));

            (_, _, string finished) =
                entries.Single(e => e.Message.StartsWith("HTTP request finished", StringComparison.Ordinal));

            finished.Should().ContainEquivalentOf("418");

            finished.Should().ContainEquivalentOf("trace-under-test");
        }
    }

    [Fact]
    public async Task After_correlation_middleware_client_header_is_logged()
    {
        RecordingLoggerProvider sink = new();

        using (ILoggerFactory factory = LoggerFactory.Create(b =>
               {
                   b.AddProvider(sink);
                   b.SetMinimumLevel(LogLevel.Information);
               }))
        {
            ILogger<HttpRequestLoggingMiddleware> logger =
                factory.CreateLogger<HttpRequestLoggingMiddleware>();

            HttpRequestLoggingMiddleware logging = new(
                ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status204NoContent;

                    return Task.CompletedTask;
                },
                logger);

            CorrelationIdMiddleware corr = new(logging.InvokeAsync);

            DefaultHttpContext context = new()
            {
                TraceIdentifier = "server-fallback-if-invalid"
            };

            context.Request.Method = HttpMethods.Put;

            context.Request.Path = "/v1/widget";

            context.Request.Headers["X-Correlation-ID"] = "client-supplied-99";

            await corr.InvokeAsync(context);

            IList<(LogLevel Level, EventId EventId, string Message)> joined = sink.Entries;

            joined.Should()
                .Contain(e =>
                    e.Message.Contains("HTTP request started", StringComparison.Ordinal)
                    && e.Message.Contains("client-supplied-99", StringComparison.Ordinal));

            joined.Should()
                .Contain(e =>
                    e.Message.Contains("HTTP request finished", StringComparison.Ordinal)
                    && e.Message.Contains("client-supplied-99", StringComparison.Ordinal));
        }
    }
}
