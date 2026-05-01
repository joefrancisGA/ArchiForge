using ArchLucid.Host.Core.Middleware;

using FluentAssertions;

using Microsoft.AspNetCore.Http;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Unit tests for <see cref="CorrelationIdMiddleware" />.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class CorrelationIdMiddlewareTests
{
    [SkippableFact]
    public async Task InvokeAsync_valid_header_sets_response_and_trace_identifier()
    {
        DefaultHttpContext context = new()
        {
            TraceIdentifier = "default-trace"
        };
        context.Request.Headers["X-Correlation-ID"] = "safe-id_01";

        CorrelationIdMiddleware middleware = new(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers["X-Correlation-ID"].ToString().Should().Be("safe-id_01");
        context.TraceIdentifier.Should().Be("safe-id_01");
    }

    [SkippableFact]
    public async Task InvokeAsync_invalid_header_falls_back_to_trace_identifier()
    {
        DefaultHttpContext context = new()
        {
            TraceIdentifier = "fallback-trace"
        };
        context.Request.Headers["X-Correlation-ID"] = "bad id has spaces";

        CorrelationIdMiddleware middleware = new(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers["X-Correlation-ID"].ToString().Should().Be("fallback-trace");
        context.TraceIdentifier.Should().Be("fallback-trace");
    }

    [SkippableFact]
    public async Task InvokeAsync_invokes_next_delegate()
    {
        DefaultHttpContext context = new();
        bool nextCalled = false;
        CorrelationIdMiddleware middleware = new(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }
}
