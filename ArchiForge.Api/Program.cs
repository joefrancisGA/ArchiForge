using ArchiForge.Api.Health;
using ArchiForge.Api.Middleware;
using Serilog;
using ArchiForge.Api.Services;
using ArchiForge.Api.Validators;
using ArchiForge.Coordinator.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using ArchiForge.Data.Infrastructure;
using ArchiForge.Data.Repositories;
using ArchiForge.DecisionEngine.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArchiForge.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext());

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            }).AddMvc().AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddValidatorsFromAssemblyContaining<ArchitectureRequestValidator>();
            builder.Services.AddOpenApi();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new() { Title = "ArchiForge API", Version = "v1" });
            });

            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddFixedWindowLimiter("fixed", config =>
                {
                    config.Window = TimeSpan.FromMinutes(1);
                    config.PermitLimit = 100;
                    config.QueueLimit = 0;
                });
            });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("ArchiForge", policy =>
                {
                    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
                    if (origins.Length > 0)
                    {
                        policy.WithOrigins(origins)
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                    }
                    else
                    {
                        policy.SetIsOriginAllowed(_ => false);
                    }
                });
            });

            builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
            builder.Services.AddHealthChecks()
                .AddCheck<SqlConnectionHealthCheck>("database", failureStatus: HealthStatus.Unhealthy);
            builder.Services.AddScoped<IArchitectureApplicationService, ArchitectureApplicationService>();
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

            var connectionString = app.Configuration.GetConnectionString("ArchiForge");
            if (!string.IsNullOrEmpty(connectionString) && !DatabaseMigrator.Run(connectionString))
            {
                throw new InvalidOperationException("Database migration failed.");
            }

            app.UseMiddleware<CorrelationIdMiddleware>();
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
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ArchiForge API v1");
                });
            }

            app.UseHttpsRedirection();

            app.UseCors("ArchiForge");

            app.UseRateLimiter();

            app.UseAuthorization();

            app.MapHealthChecks("/health");
            app.MapControllers();

            app.Run();
        }
    }
}
