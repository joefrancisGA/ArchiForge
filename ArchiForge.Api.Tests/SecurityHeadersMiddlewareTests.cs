using ArchiForge.Api.Middleware;

using FluentAssertions;

using Microsoft.AspNetCore.Http;

namespace ArchiForge.Api.Tests;

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
    }
}
