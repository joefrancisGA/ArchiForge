using ArchLucid.Host.Core.Middleware;

using FluentAssertions;

using Microsoft.AspNetCore.Http;

namespace ArchLucid.Api.Tests;

/// <summary>
/// Tests for Security Headers Middleware.
/// </summary>

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_SetsBaselineSecurityHeaders_BeforeNext()
    {
        DefaultHttpContext context = new();
        bool nextInvoked = false;
        SecurityHeadersMiddleware middleware = new(_ =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        nextInvoked.Should().BeTrue();
        IHeaderDictionary headers = context.Response.Headers;
        headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
        headers["X-Frame-Options"].ToString().Should().Be("DENY");
        headers["Referrer-Policy"].ToString().Should().Be("strict-origin-when-cross-origin");
        headers["Content-Security-Policy"].ToString().Should().Be(SecurityHeadersMiddleware.ContentSecurityPolicyApiJson);
        headers["Cache-Control"].ToString().Should().Be("no-store, max-age=0");
        headers["Pragma"].ToString().Should().Be("no-cache");
        headers["Cross-Origin-Resource-Policy"].ToString().Should().Be("cross-origin");
        headers["Cross-Origin-Embedder-Policy"].ToString().Should().Be("require-corp");
        headers["Cross-Origin-Opener-Policy"].ToString().Should().Be("same-origin");
        headers["Permissions-Policy"].ToString().Should().StartWith("accelerometer=()");
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/robots.txt")]
    [InlineData("/sitemap.xml")]
    public async Task InvokeAsync_PublicCrawlerPaths_UseShortPublicCache(string path)
    {
        DefaultHttpContext context = new()
        {
            Request = { Path = path }
        };
        SecurityHeadersMiddleware middleware = new(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        IHeaderDictionary headers = context.Response.Headers;
        headers["Cache-Control"].ToString().Should().Be("public, max-age=3600");
        headers.ContainsKey("Pragma").Should().BeFalse();
        headers["Cross-Origin-Embedder-Policy"].ToString().Should().Be("require-corp");
        headers["Cross-Origin-Opener-Policy"].ToString().Should().Be("same-origin");
    }

    [Fact]
    public async Task InvokeAsync_DoesNotOverwriteExistingHeaders()
    {
        const string existingCsp = "default-src 'self'; frame-ancestors 'none'";
        DefaultHttpContext context = new();
        context.Response.Headers["Content-Security-Policy"] = existingCsp;

        SecurityHeadersMiddleware middleware = new(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        IHeaderDictionary headers = context.Response.Headers;
        headers["Content-Security-Policy"].ToString().Should().Be(existingCsp);
        headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
    }
}
