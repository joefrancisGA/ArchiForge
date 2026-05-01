using ArchLucid.Host.Core.Configuration;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Unit tests for <see cref="ApiDeprecationHeadersMiddleware" />.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ApiDeprecationHeadersMiddlewareTests
{
    [SkippableFact]
    public async Task InvokeAsync_when_disabled_does_not_register_OnStarting()
    {
        Mock<IOptionsMonitor<ApiDeprecationOptions>> options = new();
        options.Setup(o => o.CurrentValue).Returns(new ApiDeprecationOptions { Enabled = false });

        bool nextCalled = false;
        ApiDeprecationHeadersMiddleware middleware = new(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            options.Object);

        DefaultHttpContext context = new();
        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.Headers.Should().NotContainKey("Deprecation");
    }

    [SkippableFact]
    public async Task InvokeAsync_when_enabled_registers_OnStarting_and_next_runs()
    {
        Mock<IOptionsMonitor<ApiDeprecationOptions>> options = new();
        options.Setup(o => o.CurrentValue).Returns(
            new ApiDeprecationOptions
            {
                Enabled = true,
                EmitDeprecationTrue = true,
                SunsetHttpDate = "Wed, 11 Nov 2026 23:59:59 GMT",
                Link = "<https://example.com/migration>; rel=\"sunset\""
            });

        bool nextCalled = false;
        ApiDeprecationHeadersMiddleware middleware = new(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            options.Object);

        DefaultHttpContext context = new();
        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }
}
