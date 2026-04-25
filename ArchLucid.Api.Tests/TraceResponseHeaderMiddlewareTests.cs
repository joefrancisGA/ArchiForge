using System.Diagnostics;

using ArchLucid.Api.Tests.Http;
using ArchLucid.Host.Core.Middleware;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Unit tests for <see cref="TraceResponseHeaderMiddleware" />.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class TraceResponseHeaderMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ActivityCurrent_SetsTraceparentHeader()
    {
        Activity activity = new("test-traceparent");
        activity.SetIdFormat(ActivityIdFormat.W3C);

        activity.Start();

        try
        {
            TraceResponseHeaderMiddleware middleware = new(_ => Task.CompletedTask);
            DefaultHttpContext context =
                OnStartingCapturingHttpResponseFeature.CreateContext(
                    out OnStartingCapturingHttpResponseFeature capture);

            await middleware.InvokeAsync(context);
            await capture.InvokeOnStartingCallbacksAsync();

            context.Response.Headers.TryGetValue("traceparent", out StringValues tp).Should().BeTrue();
            tp.ToString().Should().Be(activity.Id);
        }
        finally
        {
            activity.Stop();
        }
    }

    [Fact]
    public async Task InvokeAsync_ActivityCurrent_SetsXTraceIdHeader()
    {
        Activity activity = new("test-x-trace-id");
        activity.SetIdFormat(ActivityIdFormat.W3C);

        activity.Start();

        try
        {
            TraceResponseHeaderMiddleware middleware = new(_ => Task.CompletedTask);
            DefaultHttpContext context =
                OnStartingCapturingHttpResponseFeature.CreateContext(
                    out OnStartingCapturingHttpResponseFeature capture);

            await middleware.InvokeAsync(context);
            await capture.InvokeOnStartingCallbacksAsync();

            context.Response.Headers.TryGetValue("X-Trace-Id", out StringValues xt).Should().BeTrue();
            xt.ToString().Should().Be(activity.TraceId.ToString());
            xt.ToString().Length.Should().Be(32);
            xt.ToString().Should().MatchRegex("^[0-9a-fA-F]{32}$");
        }
        finally
        {
            activity.Stop();
        }
    }

    [Fact]
    public async Task InvokeAsync_NoActivity_DoesNotSetHeaders()
    {
        Activity activity = new("stop-before-on-starting");
        activity.SetIdFormat(ActivityIdFormat.W3C);

        activity.Start();

        TraceResponseHeaderMiddleware middleware = new(_ =>
        {
            activity.Stop();

            return Task.CompletedTask;
        });

        DefaultHttpContext context =
            OnStartingCapturingHttpResponseFeature.CreateContext(out OnStartingCapturingHttpResponseFeature capture);

        await middleware.InvokeAsync(context);
        Activity.Current.Should().BeNull();
        await capture.InvokeOnStartingCallbacksAsync();

        context.Response.Headers.ContainsKey("traceparent").Should().BeFalse();
        context.Response.Headers.ContainsKey("X-Trace-Id").Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_InvokesNextDelegate()
    {
        DefaultHttpContext context = new();
        bool nextCalled = false;
        TraceResponseHeaderMiddleware middleware = new(_ =>
        {
            nextCalled = true;

            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }
}
