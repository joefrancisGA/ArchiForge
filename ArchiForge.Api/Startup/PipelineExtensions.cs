using ArchiForge.Api.Health;
using ArchiForge.Api.Middleware;
using ArchiForge.Api.ProblemDetails;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace ArchiForge.Api.Startup;

internal static class PipelineExtensions
{
    public static WebApplication UseArchiForgePipeline(this WebApplication app)
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
                context.Response.StatusCode = problem.Status ?? 500;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(problem);
            });
        });

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "ArchiForge API v1");
            });
        }

        if (!app.Environment.IsDevelopment())
            app.UseHsts();

        app.UseHttpsRedirection();
        app.UseResponseCompression();
        app.UseCors("ArchiForge");
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = static check => check.Tags.Contains(ReadinessTags.Live),
        });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = static check => check.Tags.Contains(ReadinessTags.Ready),
        });
        app.MapHealthChecks("/health");

        bool prometheusEnabled = app.Configuration.GetValue("Observability:Prometheus:Enabled", false);
        if (prometheusEnabled)
        {
            app.UseOpenTelemetryPrometheusScrapingEndpoint();
        }
        app.MapControllers();
        return app;
    }
}
