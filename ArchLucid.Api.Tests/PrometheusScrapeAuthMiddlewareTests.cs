using System.Text;

using ArchLucid.Host.Core.Configuration;
using ArchLucid.Host.Core.Middleware;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ArchLucid.Api.Tests;

public sealed class PrometheusScrapeAuthMiddlewareTests
{
    private static readonly RequestDelegate NextOk = static ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status200OK;

        return Task.CompletedTask;
    };

    private static IOptions<ObservabilityHostOptions> OptionsForPrometheus(ObservabilityPrometheusOptions prometheus)
    {
        return Options.Create(
            new ObservabilityHostOptions
            {
                Prometheus = prometheus,
            });
    }

    [Fact]
    public async Task InvokeAsync_when_scrape_credentials_configured_and_path_is_metrics_without_header_returns_401()
    {
        ObservabilityPrometheusOptions prometheus = new()
        {
            ScrapeUsername = "scraper",
            ScrapePassword = "secret",
            ScrapePath = "/metrics",
        };
        PrometheusScrapeAuthMiddleware middleware = new(NextOk, OptionsForPrometheus(prometheus));
        DefaultHttpContext http = new()
        {
            Request = { Method = "GET", Path = "/metrics" },
            Response = { Body = new MemoryStream() }
        };

        await middleware.InvokeAsync(http);

        http.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task InvokeAsync_when_valid_basic_auth_calls_next()
    {
        ObservabilityPrometheusOptions prometheus = new()
        {
            ScrapeUsername = "scraper",
            ScrapePassword = "secret",
            ScrapePath = "/metrics",
        };
        PrometheusScrapeAuthMiddleware middleware = new(NextOk, OptionsForPrometheus(prometheus));
        DefaultHttpContext http = new()
        {
            Request = { Method = "GET", Path = "/metrics" },
            Response = { Body = new MemoryStream() }
        };
        string basic = Convert.ToBase64String(Encoding.UTF8.GetBytes("scraper:secret"));
        http.Request.Headers.Authorization = $"Basic {basic}";

        await middleware.InvokeAsync(http);

        http.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_when_credentials_configured_non_metrics_path_bypasses_auth()
    {
        ObservabilityPrometheusOptions prometheus = new()
        {
            ScrapeUsername = "scraper",
            ScrapePassword = "secret",
            ScrapePath = "/metrics",
        };
        PrometheusScrapeAuthMiddleware middleware = new(NextOk, OptionsForPrometheus(prometheus));
        DefaultHttpContext http = new()
        {
            Request = { Method = "GET", Path = "/health/live" },
            Response = { Body = new MemoryStream() }
        };

        await middleware.InvokeAsync(http);

        http.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_when_no_scrape_credentials_configured_metrics_path_is_open()
    {
        ObservabilityPrometheusOptions prometheus = new()
        {
            ScrapeUsername = "",
            ScrapePassword = "",
            ScrapePath = "/metrics",
        };
        PrometheusScrapeAuthMiddleware middleware = new(NextOk, OptionsForPrometheus(prometheus));
        DefaultHttpContext http = new()
        {
            Request = { Method = "GET", Path = "/metrics" },
            Response = { Body = new MemoryStream() }
        };

        await middleware.InvokeAsync(http);

        http.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_when_prometheus_enabled_require_auth_and_no_credentials_metrics_returns_401()
    {
        ObservabilityPrometheusOptions prometheus = new()
        {
            Enabled = true,
            RequireScrapeAuthentication = true,
            ScrapeUsername = "",
            ScrapePassword = "",
            ScrapePath = "/metrics",
        };
        PrometheusScrapeAuthMiddleware middleware = new(NextOk, OptionsForPrometheus(prometheus));
        DefaultHttpContext http = new()
        {
            Request = { Method = "GET", Path = "/metrics" },
            Response = { Body = new MemoryStream() }
        };

        await middleware.InvokeAsync(http);

        http.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }
}
