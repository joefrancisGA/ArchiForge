using ArchiForge.Api.Health;
using ArchiForge.Coordinator.Services;
using ArchiForge.Data.Infrastructure;
using ArchiForge.Data.Repositories;
using ArchiForge.DecisionEngine.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArchiForge.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            builder.Services.AddSingleton<SqlConnectionFactory>();
            builder.Services.AddHealthChecks()
                .AddCheck<SqlConnectionHealthCheck>("database", failureStatus: HealthStatus.Unhealthy);
            builder.Services.AddScoped<ICoordinatorService, CoordinatorService>();
            builder.Services.AddScoped<IDecisionEngineService, DecisionEngineService>();
            builder.Services.AddScoped<IArchitectureRequestRepository, ArchitectureRequestRepository>();
            builder.Services.AddScoped<IArchitectureRunRepository, ArchitectureRunRepository>();
            builder.Services.AddScoped<IAgentTaskRepository, AgentTaskRepository>();
            builder.Services.AddScoped<IAgentResultRepository, AgentResultRepository>();
            builder.Services.AddScoped<IGoldenManifestRepository, GoldenManifestRepository>();
            builder.Services.AddScoped<IEvidenceBundleRepository, EvidenceBundleRepository>();
            builder.Services.AddScoped<IDecisionTraceRepository, DecisionTraceRepository>();

            var app = builder.Build();

            app.UseExceptionHandler(exceptionHandlerApp =>
            {
                exceptionHandlerApp.Run(async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "An unexpected error occurred.",
                        requestId = context.TraceIdentifier
                    });
                });
            });

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapHealthChecks("/health");
            app.MapControllers();

            app.Run();
        }
    }
}
