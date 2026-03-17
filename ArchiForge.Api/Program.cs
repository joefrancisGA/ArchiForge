using ArchiForge.AgentRuntime;
using ArchiForge.AgentSimulator.Services;
using ArchiForge.Api.Authentication;
using ArchiForge.Api.Jobs;
using ArchiForge.Api.Health;
using ArchiForge.Api.Middleware;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Api.Services;
using ArchiForge.Api.Validators;
using ArchiForge.Api.Startup.Validation;
using ArchiForge.Application;
using ArchiForge.Application.Agents;
using ArchiForge.Application.Analysis;
using ArchiForge.Application.Determinism;
using ArchiForge.Application.Diagrams;
using ArchiForge.Application.Diffs;
using ArchiForge.Application.Evidence;
using ArchiForge.Application.Exports;
using ArchiForge.Application.Summaries;
using ArchiForge.Contracts.Requests;
using ArchiForge.Coordinator.Services;
using ArchiForge.Data.Infrastructure;
using ArchiForge.Data.Repositories;
using ArchiForge.DecisionEngine.Services;
using Asp.Versioning;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Text.Json;

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
                c.OperationFilter<ArchiForge.Api.Swagger.ReplayExamplesOperationFilter>();
            });

            builder.Services.AddAuthentication("ApiKey")
                .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", options => { });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("CanCommitRuns", policy =>
                    policy.RequireClaim("permission", "commit:run"));

                options.AddPolicy("CanSeedResults", policy =>
                    policy.RequireClaim("permission", "seed:results"));

                options.AddPolicy("CanExportConsultingDocx", policy =>
                    policy.RequireClaim("permission", "export:consulting-docx"));

                options.AddPolicy("CanReplayComparisons", policy =>
                    policy.RequireClaim("permission", "replay:comparisons"));

                options.AddPolicy("CanViewReplayDiagnostics", policy =>
                    policy.RequireClaim("permission", "replay:diagnostics"));
            });

            var prometheusEnabled = builder.Configuration.GetValue("Observability:Prometheus:Enabled", false);
            var consoleExporterEnabled = builder.Configuration.GetValue("Observability:ConsoleExporter:Enabled", builder.Environment.IsDevelopment());

            builder.Services.AddOpenTelemetry()
                .ConfigureResource(resource => resource
                    .AddService(
                        serviceName: "ArchiForge.Api",
                        serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
                        serviceInstanceId: Environment.MachineName))
                .WithTracing(tracing =>
                {
                    tracing.AddAspNetCoreInstrumentation();
                    tracing.AddHttpClientInstrumentation();
                    tracing.AddSqlClientInstrumentation();
                    if (consoleExporterEnabled)
                    {
                        tracing.AddConsoleExporter();
                    }
                })
                .WithMetrics(metrics =>
                {
                    metrics.AddAspNetCoreInstrumentation();
                    metrics.AddHttpClientInstrumentation();
                    if (prometheusEnabled)
                    {
                        metrics.AddPrometheusExporter();
                    }
                });

            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                var fixedPermitLimit = builder.Configuration.GetValue("RateLimiting:FixedWindow:PermitLimit", 100);
                var fixedWindowMinutes = builder.Configuration.GetValue("RateLimiting:FixedWindow:WindowMinutes", 1);
                var fixedQueueLimit = builder.Configuration.GetValue("RateLimiting:FixedWindow:QueueLimit", 0);

                options.AddFixedWindowLimiter("fixed", config =>
                {
                    config.Window = TimeSpan.FromMinutes(fixedWindowMinutes);
                    config.PermitLimit = fixedPermitLimit;
                    config.QueueLimit = fixedQueueLimit;
                });

                var expensivePermitLimit = builder.Configuration.GetValue("RateLimiting:Expensive:PermitLimit", 20);
                var expensiveWindowMinutes = builder.Configuration.GetValue("RateLimiting:Expensive:WindowMinutes", 1);
                var expensiveQueueLimit = builder.Configuration.GetValue("RateLimiting:Expensive:QueueLimit", 0);

                options.AddFixedWindowLimiter("expensive", config =>
                {
                    config.Window = TimeSpan.FromMinutes(expensiveWindowMinutes);
                    config.PermitLimit = expensivePermitLimit;
                    config.QueueLimit = expensiveQueueLimit;
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
            builder.Services.AddSingleton<IBackgroundJobQueue, InMemoryBackgroundJobQueue>();
            builder.Services.AddHostedService(sp => (InMemoryBackgroundJobQueue)sp.GetRequiredService<IBackgroundJobQueue>());
            builder.Services.AddScoped<IRunExportRecordRepository, RunExportRecordRepository>();
            builder.Services.AddScoped<IRunExportAuditService, RunExportAuditService>();
            builder.Services.AddScoped<IArchitectureApplicationService, ArchitectureApplicationService>();
            builder.Services.AddScoped<IArchitectureAnalysisService, ArchitectureAnalysisService>();
            builder.Services.AddScoped<IArchitectureAnalysisExportService, MarkdownArchitectureAnalysisExportService>();
            builder.Services.AddScoped<IDiagramImageRenderer, NullDiagramImageRenderer>();
            builder.Services.AddScoped<IArchitectureAnalysisDocxExportService, DocxArchitectureAnalysisExportService>();
            builder.Services.Configure<ConsultingDocxTemplateOptions>(
                builder.Configuration.GetSection("ConsultingDocxTemplate"));
            builder.Services.AddScoped<IConsultingDocxTemplateOptionsProvider, DefaultConsultingDocxTemplateOptionsProvider>();
            builder.Services.AddScoped<IDocumentLogoProvider, FileSystemDocumentLogoProvider>();
            builder.Services.AddScoped<IArchitectureAnalysisConsultingDocxExportService, ConsultingDocxArchitectureAnalysisExportService>();
            builder.Services.AddSingleton<IConsultingDocxTemplateProfileResolver, DefaultConsultingDocxTemplateProfileResolver>();
            builder.Services.AddScoped<IConsultingDocxTemplateRecommendationService, ConsultingDocxTemplateRecommendationService>();
            builder.Services.AddScoped<IConsultingDocxExportProfileSelector, ConsultingDocxExportProfileSelector>();
            builder.Services.AddScoped<IEndToEndReplayComparisonService, EndToEndReplayComparisonService>();
            builder.Services.AddScoped<IEndToEndReplayComparisonSummaryFormatter, MarkdownEndToEndReplayComparisonSummaryFormatter>();
            builder.Services.AddScoped<IEndToEndReplayComparisonExportService, EndToEndReplayComparisonExportService>();
            builder.Services.AddScoped<IComparisonRecordRepository, ComparisonRecordRepository>();
            builder.Services.AddScoped<IComparisonAuditService, ComparisonAuditService>();
            builder.Services.AddScoped<IComparisonDriftAnalyzer, ComparisonDriftAnalyzer>();
            builder.Services.AddScoped<IComparisonReplayService, ComparisonReplayService>();
            builder.Services.AddSingleton<ArchiForge.Api.Services.IReplayDiagnosticsRecorder, ArchiForge.Api.Services.ReplayDiagnosticsRecorder>();
            builder.Services.AddScoped<IArchitectureRunService, ArchitectureRunService>();
            builder.Services.AddScoped<IReplayRunService, ReplayRunService>();
            builder.Services.AddScoped<IDeterminismCheckService, DeterminismCheckService>();
            builder.Services.AddScoped<IExportReplayService, ExportReplayService>();
            builder.Services.AddScoped<IAgentExecutorResolver, DefaultAgentExecutorResolver>();
            builder.Services.AddScoped<IDiagramGenerator, MermaidDiagramGenerator>();
            builder.Services.AddScoped<IEvidenceSummaryFormatter, MarkdownEvidenceSummaryFormatter>();
            builder.Services.AddScoped<IManifestSummaryGenerator, MarkdownManifestSummaryGenerator>();
            builder.Services.AddScoped<IArchitectureExportService, MarkdownArchitectureExportService>();
            builder.Services.AddScoped<IManifestDiffService, ManifestDiffService>();
            builder.Services.AddScoped<IManifestDiffSummaryFormatter, MarkdownManifestDiffSummaryFormatter>();
            builder.Services.AddScoped<IManifestDiffExportService, MarkdownManifestDiffExportService>();
            builder.Services.AddScoped<IAgentResultDiffService, AgentResultDiffService>();
            builder.Services.AddScoped<IAgentResultDiffSummaryFormatter, MarkdownAgentResultDiffSummaryFormatter>();
            builder.Services.AddScoped<IExportRecordDiffService, ExportRecordDiffService>();
            builder.Services.AddScoped<IExportRecordDiffSummaryFormatter, MarkdownExportRecordDiffSummaryFormatter>();
            builder.Services.AddScoped<IExportRecordDiffExportService, ExportRecordDiffExportService>();
            builder.Services.AddScoped<ArchiForge.Application.Analysis.IDriftReportFormatter, ArchiForge.Application.Analysis.MarkdownDriftReportFormatter>();
            builder.Services.AddScoped<ArchiForge.Application.Analysis.DriftReportDocxExport>();
            builder.Services.AddScoped<ICoordinatorService, CoordinatorService>();
            builder.Services.AddScoped<IDecisionEngineService, DecisionEngineService>();
            builder.Services.AddScoped<IDecisionEngineV2, DecisionEngineV2>();
            builder.Services.AddScoped<ArchiForge.Application.Decisions.IAgentEvaluationService, ArchiForge.Application.Decisions.DefaultAgentEvaluationService>();
            builder.Services.AddScoped<IAgentEvaluationRepository, AgentEvaluationRepository>();
            builder.Services.AddScoped<IDecisionNodeRepository, DecisionNodeRepository>();
            builder.Services.AddScoped<IEvidenceBuilder, DefaultEvidenceBuilder>();
            builder.Services.AddScoped<IArchitectureRequestRepository, ArchitectureRequestRepository>();
            builder.Services.AddScoped<IArchitectureRunRepository, ArchitectureRunRepository>();
            builder.Services.AddScoped<IAgentTaskRepository, AgentTaskRepository>();
            builder.Services.AddScoped<IAgentResultRepository, AgentResultRepository>();
            builder.Services.AddScoped<IGoldenManifestRepository, GoldenManifestRepository>();
            builder.Services.AddScoped<IEvidenceBundleRepository, EvidenceBundleRepository>();
            builder.Services.AddScoped<IDecisionTraceRepository, DecisionTraceRepository>();
            builder.Services.AddScoped<IAgentEvidencePackageRepository, AgentEvidencePackageRepository>();
            builder.Services.AddScoped<IAgentExecutionTraceRepository, AgentExecutionTraceRepository>();
            builder.Services.AddScoped<IAgentExecutionTraceRecorder, AgentExecutionTraceRecorder>();
            builder.Services.AddHostedService<ConfigurationValidator>();

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

                var azureOpenAiEndpoint = builder.Configuration["AzureOpenAI:Endpoint"];
                var azureOpenAiKey = builder.Configuration["AzureOpenAI:ApiKey"];
                var azureOpenAiDeployment = builder.Configuration["AzureOpenAI:DeploymentName"];
                var useAzureOpenAi = !string.IsNullOrWhiteSpace(azureOpenAiEndpoint)
                    && !string.IsNullOrWhiteSpace(azureOpenAiKey)
                    && !string.IsNullOrWhiteSpace(azureOpenAiDeployment);

                if (useAzureOpenAi)
                {
                    builder.Services.AddSingleton<IAgentCompletionClient>(sp =>
                    {
                        var configuration = sp.GetRequiredService<IConfiguration>();
                        var endpoint = configuration["AzureOpenAI:Endpoint"]
                            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is missing.");
                        var apiKey = configuration["AzureOpenAI:ApiKey"]
                            ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is missing.");
                        var deploymentName = configuration["AzureOpenAI:DeploymentName"]
                            ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName is missing.");
                        return new AzureOpenAiCompletionClient(endpoint, apiKey, deploymentName);
                    });
                }
                else
                {
                    var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
                    builder.Services.AddScoped<IAgentCompletionClient>(_ => new FakeAgentCompletionClient(
                        (_, userPrompt) =>
                        {
                            var runId = "RUN-001";
                            var taskId = "TASK-TOPO-001";
                            foreach (var line in userPrompt.Split('\n'))
                            {
                                var span = line.AsSpan().Trim();
                                if (span.StartsWith("RunId:", StringComparison.OrdinalIgnoreCase))
                                    runId = span.Length > 6 ? span[6..].Trim().ToString() : runId;
                                else if (span.StartsWith("TaskId:", StringComparison.OrdinalIgnoreCase))
                                    taskId = span.Length > 7 ? span[7..].Trim().ToString() : taskId;
                            }
                            var dummyRequest = new ArchitectureRequest
                            {
                                SystemName = "Default",
                                Description = "Default request for fake topology response.",
                                Environment = "prod"
                            };
                            var result = FakeScenarioFactory.CreateTopologyResult(runId, taskId, dummyRequest);
                            return JsonSerializer.Serialize(result, jsonOptions);
                        }));
                }
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

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHealthChecks("/health");
            if (prometheusEnabled)
            {
                app.UseOpenTelemetryPrometheusScrapingEndpoint();
            }
            app.MapControllers();

            app.Run();
        }
    }
}

public partial class Program { }
