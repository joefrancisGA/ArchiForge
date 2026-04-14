using System.Diagnostics;

using ArchLucid.Api.Auth.Models;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Api.Middleware;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Host.Core.Health;
using ArchLucid.Host.Core.Hosting;
using ArchLucid.Host.Core.Middleware;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace ArchLucid.Api.Startup;

internal static class PipelineExtensions
{
    public static WebApplication UseArchLucidPipeline(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<TraceResponseHeaderMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<ApiDeprecationHeadersMiddleware>();
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
                        logger.LogError(
                            ex,
                            "Unhandled exception for {Method} {Path}",
                            LogSanitizer.Sanitize(context.Request.Method),
                            LogSanitizer.Sanitize(context.Request.Path.Value));
                    }
                }

                Microsoft.AspNetCore.Mvc.ProblemDetails problem = new()
                {
                    Type = ProblemTypes.InternalError,
                    Title = "An unexpected error occurred.",
                    Status = StatusCodes.Status500InternalServerError,
                    Detail =
                        "An unhandled exception has occurred. Use the correlationId value in this response (and the X-Correlation-ID header) when contacting support.",
                    Instance = context.Request.Path,
                    Extensions =
                    {
                        ["traceId"] = context.TraceIdentifier,
                        ["traceParent"] = Activity.Current?.Id,
                    }
                };
                ProblemErrorCodes.AttachErrorCode(problem, ProblemTypes.InternalError);
                ProblemSupportHints.AttachForProblemType(problem);
                ProblemCorrelation.Attach(problem, context);
                context.Response.StatusCode = problem.Status ?? 500;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(problem);
            });
        });

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi().AllowAnonymous();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "ArchLucid API v1");
            });
        }

        if (!app.Environment.IsDevelopment())
            app.UseHsts();

        if (AspNetCoreHostingUrls.ShouldUseHttpsRedirection(app.Configuration))
            app.UseHttpsRedirection();
        app.UseResponseCompression();
        app.UseCors("ArchLucid");
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = static check => check.Tags.Contains(ReadinessTags.Live),
        })
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
                DetailedHealthCheckResponseWriter.WriteAsync(ctx, r, HealthCheckResponseDetailLevel.Detailed),
        })
            .RequireAuthorization(ArchLucidPolicies.ReadAuthority);

        // OWASP ZAP baseline (and similar spiders) expect 200 on the scan root, /robots.txt, and /sitemap.xml.
        // Without these, the automation plan fails on 404s; SecurityHeadersMiddleware uses short public caching on these paths to satisfy passive 10049-1.
        app.MapGet(
                "/",
                static () => Results.Text("ArchLucid API", "text/plain", statusCode: StatusCodes.Status200OK))
            .AllowAnonymous();
        app.MapGet(
                "/robots.txt",
                static () => Results.Text(
                    "User-agent: *\nAllow: /\n",
                    "text/plain",
                    statusCode: StatusCodes.Status200OK))
            .AllowAnonymous();
        app.MapGet(
                "/sitemap.xml",
                static () => Results.Content(
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?><urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\"/>",
                    "application/xml",
                    statusCode: StatusCodes.Status200OK))
            .AllowAnonymous();

        bool prometheusEnabled = app.Configuration.GetValue("Observability:Prometheus:Enabled", false);
        if (prometheusEnabled)
        {
            app.UseMiddleware<PrometheusScrapeAuthMiddleware>();
            app.UseOpenTelemetryPrometheusScrapingEndpoint();
        }

        app.MapControllers();
        return app;
    }
}
