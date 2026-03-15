using System.Text.Json;
using ArchiForge.AgentSimulator.Services;
using ArchiForge.Api.Health;
using ArchiForge.Api.Middleware;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Api.Services;
using ArchiForge.Api.Validators;
using ArchiForge.Contracts.Requests;
using ArchiForge.Coordinator.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using ArchiForge.Data.Infrastructure;
using ArchiForge.Data.Repositories;
using ArchiForge.DecisionEngine.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ArchiForge.AgentRuntime;
using ArchiForge.Application;
using ArchiForge.Application.Diagrams;

namespace ArchiForge.Api
{
    public partial class Program
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
            builder.Services.AddProblemDetails();
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
            builder.Services.AddScoped<IArchitectureRunService, ArchitectureRunService>();
            builder.Services.AddScoped<IDiagramGenerator, MermaidDiagramGenerator>();
            builder.Services.AddScoped<ICoordinatorService, CoordinatorService>();
            builder.Services.AddScoped<IDecisionEngineService, DecisionEngineService>();
            builder.Services.AddScoped<IArchitectureRequestRepository, ArchitectureRequestRepository>();
            builder.Services.AddScoped<IArchitectureRunRepository, ArchitectureRunRepository>();
            builder.Services.AddScoped<IAgentTaskRepository, AgentTaskRepository>();
            builder.Services.AddScoped<IAgentResultRepository, AgentResultRepository>();
            builder.Services.AddScoped<IGoldenManifestRepository, GoldenManifestRepository>();
            builder.Services.AddScoped<IEvidenceBundleRepository, EvidenceBundleRepository>();
            builder.Services.AddScoped<IDecisionTraceRepository, DecisionTraceRepository>();

            var agentMode = builder.Configuration["AgentExecution:Mode"];
            if (string.Equals(agentMode, "Simulator", StringComparison.OrdinalIgnoreCase))
            {
                builder.Services.AddScoped<IAgentExecutor, DeterministicAgentSimulator>();
            }
            else
            {
                builder.Services.AddScoped<IAgentExecutor, RealAgentExecutor>();
                builder.Services.AddScoped<IAgentHandler, TopologyAgentHandler>();
                builder.Services.AddScoped<IAgentHandler, CostAgentHandler>();
                builder.Services.AddScoped<IAgentHandler, ComplianceAgentHandler>();
                builder.Services.AddScoped<IAgentHandler, CriticAgentHandler>();
                builder.Services.AddScoped<IAgentResultParser, AgentResultParser>();
                // For production with LLM, replace FakeAgentCompletionClient with AzureOpenAiCompletionClient.
                var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
                builder.Services.AddScoped<IAgentCompletionClient>(_ => new FakeAgentCompletionClient(
                    (_, __, runId, taskId) =>
                    {
                        var dummyRequest = new ArchitectureRequest
                        {
                            SystemName = "Default",
                            Description = "Default request for fake topology response.",
                            Environment = "prod"
                        };
                        var result = FakeScenarioFactory.CreateTopologyResult(
                            runId ?? string.Empty,
                            taskId ?? string.Empty,
                            dummyRequest);
                        return JsonSerializer.Serialize(result, jsonOptions);
                    }));
            }

            builder.Services.AddScoped<ArchitectureRunOrchestrator>();

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
                    var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
                    {
                        Type = ProblemTypes.InternalError,
                        Title = "An unexpected error occurred.",
                        Status = StatusCodes.Status500InternalServerError,
                        Detail = "An unhandled exception has occurred. Use the trace identifier when contacting support.",
                        Instance = context.Request.Path,
                        Extensions = { ["traceId"] = context.TraceIdentifier }
                    };
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

public partial class Program { }
