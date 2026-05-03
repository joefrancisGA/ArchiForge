using ArchLucid.Core.Diagnostics;
using ArchLucid.Host.Core.Health;
using ArchLucid.Host.Core.Hosting;
using ArchLucid.Host.Core.Middleware;
using ArchLucid.Host.Core.ProblemDetails;

namespace ArchLucid.Host.Core.Startup;

/// <summary>HTTP pipeline for the background Worker web host (health + observability only).</summary>
public static class WorkerHostPipelineExtensions
{
    /// <summary>
    /// Minimal pipeline for the background Worker host (<c>Hosting:Role=Worker</c>): health checks, security headers, optional Prometheus.
    /// </summary>
    /// <remarks>
    /// Worker has no API authentication stack. <c>/health/live</c> stays minimal; <c>/health/ready</c> and <c>/health</c> use
    /// summary JSON only. For full diagnostic health (build version, exception text), call the API host <c>GET /health</c>
    /// with an authenticated principal that satisfies the API <c>ReadAuthority</c> policy.
    /// </remarks>
    public static WebApplication UseArchLucidWorkerPipeline(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<TraceResponseHeaderMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseExceptionHandler(exceptionHandlerApp =>
        {
            exceptionHandlerApp.Run(async context =>
            {
                IExceptionHandlerFeature? exceptionFeature = context.Features
                    .Get<IExceptionHandlerFeature>();

                if (exceptionFeature?.Error is { } ex)
                {
                    ILogger<WebApplication> logger = context.RequestServices
                        .GetRequiredService<ILogger<WebApplication>>();

                    if (logger.IsEnabled(LogLevel.Error))
                    {
                        logger.LogErrorUnhandledWorkerHttpRequest(
                            ex,
                            context.Request.Method,
                            context.Request.Path
                                .Value); // codeql[cs/log-forging]: user-derived method/path are normalized in LogErrorUnhandledWorkerHttpRequest (CWE-117, LogSanitizer; see SanitizedLoggerErrorExtensions and docs/library/CODEQL_TRIAGE.md).
                    }
                }

                Microsoft.AspNetCore.Mvc.ProblemDetails problem = new()
                {
                    Type = ProblemTypes.InternalError,
                    Title = "An unexpected error occurred.",
                    Status = StatusCodes.Status500InternalServerError,
                    Detail = "An unhandled exception has occurred. Use the trace identifier when contacting support.",
                    Instance = context.Request.Path,
                    Extensions = { ["traceId"] = context.TraceIdentifier }
                };
                ProblemErrorCodes.AttachErrorCode(problem, ProblemTypes.InternalError);
                ProblemSupportHints.AttachForProblemType(problem);
                context.Response.StatusCode = problem.Status ?? 500;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(problem);
            });
        });

        if (!app.Environment.IsDevelopment())
            app.UseHsts();

        if (AspNetCoreHostingUrls.ShouldUseHttpsRedirection(app.Configuration))
            app.UseHttpsRedirection();

        bool prometheusEnabled = app.Configuration.GetValue("Observability:Prometheus:Enabled", false);
        if (prometheusEnabled)
        {
            app.UseMiddleware<PrometheusScrapeAuthMiddleware>();
            app.UseOpenTelemetryPrometheusScrapingEndpoint();
        }

        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = static check => check.Tags.Contains(ReadinessTags.Live), })
            .AllowAnonymous();
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = static check => check.Tags.Contains(ReadinessTags.Ready),
                ResponseWriter = static (ctx, r) =>
                    DetailedHealthCheckResponseWriter.WriteAsync(ctx, r, HealthCheckResponseDetailLevel.Summary),
            })
            .AllowAnonymous();
        app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = static (ctx, r) =>
                    DetailedHealthCheckResponseWriter.WriteAsync(ctx, r, HealthCheckResponseDetailLevel.Summary),
            })
            .AllowAnonymous();

        return app;
    }
}
