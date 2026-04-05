using ArchiForge.Host.Core.Health;
using ArchiForge.Host.Core.Middleware;
using ArchiForge.Host.Core.ProblemDetails;

namespace ArchiForge.Host.Core.Startup;

/// <summary>HTTP pipeline for the background Worker web host (health + observability only).</summary>
public static class WorkerHostPipelineExtensions
{
    /// <summary>
    /// Minimal pipeline for the background Worker host (<c>Hosting:Role=Worker</c>): health checks, security headers, optional Prometheus.
    /// </summary>
    public static WebApplication UseArchiForgeWorkerPipeline(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
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
                    logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                        context.Request.Method, context.Request.Path);
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

        app.UseHttpsRedirection();

        bool prometheusEnabled = app.Configuration.GetValue("Observability:Prometheus:Enabled", false);
        if (prometheusEnabled)
        {
            app.UseMiddleware<PrometheusScrapeAuthMiddleware>();
            app.UseOpenTelemetryPrometheusScrapingEndpoint();
        }


        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = static check => check.Tags.Contains(ReadinessTags.Live),
        })
            .AllowAnonymous();
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = static check => check.Tags.Contains(ReadinessTags.Ready),
            ResponseWriter = DetailedHealthCheckResponseWriter.WriteAsync,
        })
            .AllowAnonymous();
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = DetailedHealthCheckResponseWriter.WriteAsync,
        })
            .AllowAnonymous();

        return app;
    }
}
