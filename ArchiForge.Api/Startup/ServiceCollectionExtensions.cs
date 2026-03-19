using ArchiForge.AgentRuntime;
using ArchiForge.AgentSimulator.Services;
using ArchiForge.Api.Health;
using ArchiForge.Api.Jobs;
using ArchiForge.Api.Services;
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
using ArchiForge.DecisionEngine.Validation;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace ArchiForge.Api.Startup;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddArchiForgeApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
        services.AddHealthChecks()
            .AddCheck<SqlConnectionHealthCheck>("database", failureStatus: HealthStatus.Unhealthy);
        services.AddSingleton<IBackgroundJobQueue, InMemoryBackgroundJobQueue>();
        services.AddHostedService(sp => (InMemoryBackgroundJobQueue)sp.GetRequiredService<IBackgroundJobQueue>());
        services.AddScoped<IRunExportRecordRepository, RunExportRecordRepository>();
        services.AddScoped<IRunExportAuditService, RunExportAuditService>();
        services.AddScoped<IArchitectureApplicationService, ArchitectureApplicationService>();
        services.AddScoped<IArchitectureAnalysisService, ArchitectureAnalysisService>();
        services.AddScoped<IArchitectureAnalysisExportService, MarkdownArchitectureAnalysisExportService>();
        services.AddScoped<IDiagramImageRenderer, NullDiagramImageRenderer>();
        services.AddScoped<IArchitectureAnalysisDocxExportService, DocxArchitectureAnalysisExportService>();
        services.Configure<ConsultingDocxTemplateOptions>(configuration.GetSection("ConsultingDocxTemplate"));
        services.AddScoped<IConsultingDocxTemplateOptionsProvider, DefaultConsultingDocxTemplateOptionsProvider>();
        services.AddScoped<IDocumentLogoProvider, FileSystemDocumentLogoProvider>();
        services.AddScoped<IArchitectureAnalysisConsultingDocxExportService, ConsultingDocxArchitectureAnalysisExportService>();
        services.AddSingleton<IConsultingDocxTemplateProfileResolver, DefaultConsultingDocxTemplateProfileResolver>();
        services.AddScoped<IConsultingDocxTemplateRecommendationService, ConsultingDocxTemplateRecommendationService>();
        services.AddScoped<IConsultingDocxExportProfileSelector, ConsultingDocxExportProfileSelector>();
        services.AddScoped<IEndToEndReplayComparisonService, EndToEndReplayComparisonService>();
        services.AddScoped<IEndToEndReplayComparisonSummaryFormatter, MarkdownEndToEndReplayComparisonSummaryFormatter>();
        services.AddScoped<IEndToEndReplayComparisonExportService, EndToEndReplayComparisonExportService>();
        services.AddScoped<IComparisonRecordRepository, ComparisonRecordRepository>();
        services.AddScoped<IComparisonAuditService, ComparisonAuditService>();
        services.AddScoped<IComparisonDriftAnalyzer, ComparisonDriftAnalyzer>();
        services.AddScoped<IComparisonReplayService, ComparisonReplayService>();
        services.AddScoped<IComparisonReplayApiService, ComparisonReplayApiService>();
        services.AddScoped<IComparisonDriftReportExportService, ComparisonDriftReportExportService>();
        services.AddSingleton<IReplayDiagnosticsRecorder, ReplayDiagnosticsRecorder>();
        services.AddScoped<IArchitectureRunService, ArchitectureRunService>();
        services.AddScoped<IReplayRunService, ReplayRunService>();
        services.AddScoped<IDeterminismCheckService, DeterminismCheckService>();
        services.AddScoped<IExportReplayService, ExportReplayService>();
        services.AddScoped<IAgentExecutorResolver, DefaultAgentExecutorResolver>();
        services.AddScoped<IDiagramGenerator, MermaidDiagramGenerator>();
        services.AddScoped<IManifestDiagramService, ManifestDiagramService>();
        services.AddScoped<IEvidenceSummaryFormatter, MarkdownEvidenceSummaryFormatter>();
        services.AddScoped<IManifestSummaryGenerator, MarkdownManifestSummaryGenerator>();
        services.AddScoped<IManifestSummaryService, ManifestSummaryService>();
        services.AddScoped<IArchitectureExportService, MarkdownArchitectureExportService>();
        services.AddScoped<IManifestDiffService, ManifestDiffService>();
        services.AddScoped<IManifestDiffSummaryFormatter, MarkdownManifestDiffSummaryFormatter>();
        services.AddScoped<IManifestDiffExportService, MarkdownManifestDiffExportService>();
        services.AddScoped<IAgentResultDiffService, AgentResultDiffService>();
        services.AddScoped<IAgentResultDiffSummaryFormatter, MarkdownAgentResultDiffSummaryFormatter>();
        services.AddScoped<IExportRecordDiffService, ExportRecordDiffService>();
        services.AddScoped<IExportRecordDiffSummaryFormatter, MarkdownExportRecordDiffSummaryFormatter>();
        services.AddScoped<IExportRecordDiffExportService, ExportRecordDiffExportService>();
        services.AddScoped<IDriftReportFormatter, MarkdownDriftReportFormatter>();
        services.AddScoped<DriftReportDocxExport>();
        services.AddSingleton<ContextIngestion.Interfaces.IContextConnector, ContextIngestion.Connectors.StaticRequestContextConnector>();
        services.AddSingleton<ContextIngestion.Interfaces.IContextSnapshotRepository, ContextIngestion.Repositories.InMemoryContextSnapshotRepository>();
        services.AddScoped<ContextIngestion.Interfaces.IContextIngestionService, ArchiForge.ContextIngestion.Services.ContextIngestionService>();
        services.AddSingleton<KnowledgeGraph.Interfaces.IGraphSnapshotRepository, KnowledgeGraph.Repositories.InMemoryGraphSnapshotRepository>();
        services.AddScoped<KnowledgeGraph.Interfaces.IGraphBuilder, KnowledgeGraph.Builders.DefaultGraphBuilder>();
        services.AddScoped<KnowledgeGraph.Interfaces.IKnowledgeGraphService, ArchiForge.KnowledgeGraph.Services.KnowledgeGraphService>();
        services.AddSingleton<Decisioning.Interfaces.IFindingsSnapshotRepository, Decisioning.Repositories.InMemoryFindingsSnapshotRepository>();
        services.AddSingleton<ArchiForge.Decisioning.Interfaces.IGoldenManifestRepository, Decisioning.Repositories.InMemoryGoldenManifestRepository>();
        services.AddSingleton<ArchiForge.Decisioning.Interfaces.IDecisionTraceRepository, Decisioning.Repositories.InMemoryDecisionTraceRepository>();
        services.AddScoped<Decisioning.Interfaces.IFindingEngine, ArchiForge.Decisioning.Services.RequirementFindingEngine>();
        services.AddScoped<Decisioning.Interfaces.IFindingEngine, ArchiForge.Decisioning.Services.TopologySanityFindingEngine>();
        services.AddScoped<Decisioning.Interfaces.IFindingEngine, ArchiForge.Decisioning.Services.SecurityBaselineFindingEngine>();
        services.AddScoped<Decisioning.Interfaces.IFindingEngine, ArchiForge.Decisioning.Services.CostConstraintFindingEngine>();
        services.AddScoped<Decisioning.Interfaces.IFindingsOrchestrator, ArchiForge.Decisioning.Services.FindingsOrchestrator>();
        services.AddSingleton<Decisioning.Interfaces.IFindingPayloadValidator, ArchiForge.Decisioning.Services.FindingPayloadValidator>();
        services.AddSingleton<Decisioning.Interfaces.IDecisionRuleProvider, Decisioning.Rules.InMemoryDecisionRuleProvider>();
        services.AddScoped<Decisioning.Interfaces.IGoldenManifestBuilder, Decisioning.Manifest.Builders.DefaultGoldenManifestBuilder>();
        services.AddSingleton<Decisioning.Interfaces.IGoldenManifestValidator, ArchiForge.Decisioning.Services.GoldenManifestValidator>();
        services.AddScoped<Decisioning.Interfaces.IDecisionEngine, ArchiForge.Decisioning.Services.RuleBasedDecisionEngine>();
        services.AddScoped<ICoordinatorService, CoordinatorService>();
        services.AddSchemaValidation(configuration);
        services.AddScoped<IDecisionEngineService, DecisionEngineService>();
        services.AddScoped<IDecisionEngineV2, DecisionEngineV2>();
        services.AddScoped<Application.Decisions.IAgentEvaluationService, Application.Decisions.DefaultAgentEvaluationService>();
        services.AddScoped<IAgentEvaluationRepository, AgentEvaluationRepository>();
        services.AddScoped<IDecisionNodeRepository, DecisionNodeRepository>();
        services.AddScoped<IEvidenceBuilder, DefaultEvidenceBuilder>();
        services.AddScoped<IArchitectureRequestRepository, ArchitectureRequestRepository>();
        services.AddScoped<IArchitectureRunRepository, ArchitectureRunRepository>();
        services.AddScoped<IAgentTaskRepository, AgentTaskRepository>();
        services.AddScoped<IAgentResultRepository, AgentResultRepository>();
        services.AddScoped<IGoldenManifestRepository, GoldenManifestRepository>();
        services.AddScoped<IEvidenceBundleRepository, EvidenceBundleRepository>();
        services.AddScoped<IDecisionTraceRepository, DecisionTraceRepository>();
        services.AddScoped<IAgentEvidencePackageRepository, AgentEvidencePackageRepository>();
        services.AddScoped<IAgentExecutionTraceRepository, AgentExecutionTraceRepository>();
        services.AddScoped<IAgentExecutionTraceRecorder, AgentExecutionTraceRecorder>();
        services.AddHostedService<ConfigurationValidator>();

        var agentMode = configuration["AgentExecution:Mode"];
        if (string.Equals(agentMode, "Simulator", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IAgentExecutor, DeterministicAgentSimulator>();
        }
        else
        {
            services.AddScoped<IAgentExecutor, RealAgentExecutor>();
            services.AddScoped<IAgentHandler, TopologyAgentHandler>();
            services.AddScoped<IAgentHandler, CostAgentHandler>();
            services.AddScoped<IAgentHandler, ComplianceAgentHandler>();
            services.AddScoped<IAgentHandler, CriticAgentHandler>();
            services.AddScoped<IAgentResultParser, AgentResultParser>();

            var azureOpenAiEndpoint = configuration["AzureOpenAI:Endpoint"];
            var azureOpenAiKey = configuration["AzureOpenAI:ApiKey"];
            var azureOpenAiDeployment = configuration["AzureOpenAI:DeploymentName"];
            var useAzureOpenAi = !string.IsNullOrWhiteSpace(azureOpenAiEndpoint)
                && !string.IsNullOrWhiteSpace(azureOpenAiKey)
                && !string.IsNullOrWhiteSpace(azureOpenAiDeployment);

            if (useAzureOpenAi)
            {
                services.AddSingleton<IAgentCompletionClient>(sp =>
                {
                    var config = sp.GetRequiredService<IConfiguration>();
                    var endpoint = config["AzureOpenAI:Endpoint"]
                        ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is missing.");
                    var apiKey = config["AzureOpenAI:ApiKey"]
                        ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is missing.");
                    var deploymentName = config["AzureOpenAI:DeploymentName"]
                        ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName is missing.");
                    return new AzureOpenAiCompletionClient(endpoint, apiKey, deploymentName);
                });
            }
            else
            {
                var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
                services.AddScoped<IAgentCompletionClient>(_ => new FakeAgentCompletionClient(
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

        services.AddScoped<ArchitectureRunOrchestrator>();
        return services;
    }
}
