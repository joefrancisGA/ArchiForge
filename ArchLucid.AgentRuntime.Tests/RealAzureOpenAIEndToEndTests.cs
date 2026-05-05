using ArchLucid.AgentRuntime.Prompts;
using ArchLucid.Capabilities.Cost;
using ArchLucid.Application.Runs.Coordination;
using ArchLucid.ContextIngestion.Models;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Merge;
using ArchLucid.Decisioning.Validation;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Orchestration;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.AgentRuntime.Tests;

/// <summary>
///     Optional live Azure OpenAI integration: set <c>ARCHLUCID_REAL_AOAI_TEST_ENDPOINT</c> and
///     <c>ARCHLUCID_REAL_AOAI_TEST_KEY</c>. Optional <c>ARCHLUCID_REAL_AOAI_TEST_DEPLOYMENT</c> (defaults to
///     <c>gpt-4o</c>).
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
// ReSharper disable once InconsistentNaming
public sealed class RealAzureOpenAIEndToEndTests
{
    private static bool HasLiveAzureOpenAiCredentials()
    {
        return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ARCHLUCID_REAL_AOAI_TEST_ENDPOINT"))
               && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ARCHLUCID_REAL_AOAI_TEST_KEY"));
    }

    [SkippableFact]
    public async Task Live_pipeline_topology_compliance_cost_merge_produces_non_empty_manifest()
    {
        Skip.IfNot(HasLiveAzureOpenAiCredentials(),
            "Set ARCHLUCID_REAL_AOAI_TEST_ENDPOINT and ARCHLUCID_REAL_AOAI_TEST_KEY.");

        using CancellationTokenSource deadline = new(TimeSpan.FromSeconds(120));
        CancellationToken cancellationToken = deadline.Token;

        string endpoint = Environment.GetEnvironmentVariable("ARCHLUCID_REAL_AOAI_TEST_ENDPOINT")!;
        string apiKey = Environment.GetEnvironmentVariable("ARCHLUCID_REAL_AOAI_TEST_KEY")!;
        string deployment =
            (Environment.GetEnvironmentVariable("ARCHLUCID_REAL_AOAI_TEST_DEPLOYMENT") ?? "gpt-4o").Trim();

        if (string.IsNullOrWhiteSpace(deployment))
        {
            deployment = "gpt-4o";
        }

        AzureOpenAiCompletionClient completion = new(
            endpoint.Trim(),
            apiKey.Trim(),
            deployment,
            AzureOpenAiCompletionClient.DefaultMaxCompletionTokens);

        AgentResultParser parser = new();
        LiveAoaiTraceSpy traceSpy = new();
        IAgentSystemPromptCatalog promptCatalog = AgentPromptCatalogTestFactory.Create();

        Mock<IAuditService> audit = new();
        audit.Setup(a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(
            new ScopeContext
            {
                TenantId = ScopeIds.DefaultTenant,
                WorkspaceId = ScopeIds.DefaultWorkspace,
                ProjectId = ScopeIds.DefaultProject
            });

        IOptionsMonitor<AgentSchemaRemediationOptions> schemaRemediation =
            AgentSchemaRemediationOptionsMonitorTestFactory.Create();

        TopologyAgentHandler topology = new(
            completion,
            parser,
            traceSpy,
            promptCatalog,
            audit.Object,
            scopeProvider.Object,
            schemaRemediation);

        ComplianceAgentHandler compliance = new(
            completion,
            parser,
            traceSpy,
            promptCatalog,
            audit.Object,
            scopeProvider.Object,
            schemaRemediation);

        CostAgentHandler cost = new();

        IOptions<AgentExecutionResilienceOptions> resilience = Options.Create(
            new AgentExecutionResilienceOptions { MaxConcurrentHandlers = 0, PerHandlerTimeoutSeconds = 0 });

        RealAgentExecutor executor = new(
            [topology, compliance, cost],
            NullLogger<RealAgentExecutor>.Instance,
            new MixedModePromptMonitor(new AgentPromptCatalogOptions()),
            new FixedScopeProviderForLiveAoai(),
            new AgentHandlerConcurrencyGate(resilience),
            resilience);

        ArchitectureRequest request = new()
        {
            RequestId = "real-aoai-" + Guid.NewGuid().ToString("N"),
            SystemName = "ContosoRetailWeb",
            Description =
                "Design a 3-tier web application on Azure with SQL backend, Redis cache, and App Service frontend "
                + "(minimum ten characters for agent context).",
            Environment = "prod",
            CloudProvider = CloudProvider.Azure,
            Constraints =
            [
                "Prefer managed services",
                "Private endpoints for data tiers"
            ],
            RequiredCapabilities =
            [
                "Azure SQL",
                "Azure Cache for Redis",
                "App Service"
            ]
        };

        Mock<IRunRepository> runRepo = new();
        runRepo.Setup(r => r.GetByIdAsync(It.IsAny<ScopeContext>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RunRecord?)null);

        ArchitectureRunAuthorityCoordination coordinator = new(
            new FakeAuthorityRunOrchestratorForLiveAoai(),
            runRepo.Object,
            scopeProvider.Object,
            NullLogger<ArchitectureRunAuthorityCoordination>.Instance);

        CoordinationResult coordination = await coordinator.CreateRunAsync(request, cancellationToken);

        coordination.Success.Should().BeTrue();

        string runId = coordination.Run.RunId;

        foreach (AgentTask task in coordination.Tasks)
        {
            task.RunId = runId;
        }

        AgentEvidencePackage evidence = new()
        {
            RunId = runId,
            RequestId = request.RequestId,
            SystemName = request.SystemName,
            Environment = request.Environment,
            CloudProvider = request.CloudProvider.ToString(),
            Request = new RequestEvidence
            {
                Description = request.Description,
                Constraints = request.Constraints.ToList(),
                RequiredCapabilities = request.RequiredCapabilities.ToList(),
                Assumptions = request.Assumptions.ToList()
            }
        };

        IReadOnlyList<AgentResult> results =
            await executor.ExecuteAsync(runId, request, evidence, coordination.Tasks, cancellationToken);

        results.Should().HaveCount(3);

        foreach (AgentResult r in results)
        {
            r.Claims.Count.Should().BeGreaterThan(0);
        }

        SchemaValidationService validationService = new(
            NullLogger<SchemaValidationService>.Instance,
            Options.Create(new SchemaValidationOptions()));

        DecisionEngineService engine = new(validationService);
        DecisionMergeResult merge = engine.MergeResults(runId, request, "v1", results, [], []);

        merge.Success.Should().BeTrue();
        merge.Manifest.Services.Count.Should().BeGreaterThan(0);
        merge.Decisions.Count.Should().BeGreaterThan(0);

        bool anyCitation = traceSpy.RawResponses.Any(static s => s.Contains("evidenceRefs", StringComparison.Ordinal));
        anyCitation.Should().BeTrue("trace should include evidence references for explainability");
    }

    private sealed class LiveAoaiTraceSpy : IAgentExecutionTraceRecorder
    {
        public List<string> RawResponses
        {
            get;
        } = [];

        public Task RecordAsync(
            string runId,
            string taskId,
            AgentType agentType,
            string systemPrompt,
            string userPrompt,
            string rawResponse,
            string? parsedResultJson,
            bool parseSucceeded,
            string? errorMessage,
            AgentPromptReproMetadata? promptRepro = null,
            int? inputTokenCount = null,
            int? outputTokenCount = null,
            string? modelDeploymentName = null,
            string? modelVersion = null,
            bool isSimulatorExecution = false,
            string? failureReasonCode = null,
            CancellationToken cancellationToken = default)
        {
            RawResponses.Add(rawResponse);

            return Task.CompletedTask;
        }
    }

    private sealed class FixedScopeProviderForLiveAoai : IScopeContextProvider
    {
        public ScopeContext GetCurrentScope()
        {
            return new ScopeContext
            {
                TenantId = ScopeIds.DefaultTenant,
                WorkspaceId = ScopeIds.DefaultWorkspace,
                ProjectId = ScopeIds.DefaultProject
            };
        }
    }

    private sealed class MixedModePromptMonitor(AgentPromptCatalogOptions value)
        : IOptionsMonitor<AgentPromptCatalogOptions>
    {
        public AgentPromptCatalogOptions CurrentValue
        {
            get;
        } = value;

        public AgentPromptCatalogOptions Get(string? name)
        {
            return CurrentValue;
        }

        public IDisposable? OnChange(Action<AgentPromptCatalogOptions, string?> listener)
        {
            return null;
        }
    }

    private sealed class FakeAuthorityRunOrchestratorForLiveAoai : IAuthorityRunOrchestrator
    {
        public Task<RunRecord> ExecuteAsync(
            ContextIngestionRequest request,
            CancellationToken cancellationToken = default,
            string? evidenceBundleIdForDeferredWork = null)
        {
            _ = cancellationToken;
            _ = evidenceBundleIdForDeferredWork;
            Guid runId = Guid.NewGuid();

            return Task.FromResult(new RunRecord
            {
                RunId = runId,
                ProjectId = request.ProjectId,
                Description = request.Description,
                CreatedUtc = DateTime.UtcNow,
                ContextSnapshotId = Guid.NewGuid(),
                GraphSnapshotId = Guid.NewGuid(),
                FindingsSnapshotId = Guid.NewGuid(),
                GoldenManifestId = Guid.NewGuid(),
                DecisionTraceId = Guid.NewGuid(),
                ArtifactBundleId = Guid.NewGuid()
            });
        }

        /// <inheritdoc />
        public Task<RunRecord> CompleteQueuedAuthorityPipelineAsync(
            ContextIngestionRequest request,
            CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;

            return Task.FromResult(new RunRecord
            {
                RunId = request.RunId,
                ProjectId = request.ProjectId,
                Description = request.Description,
                CreatedUtc = DateTime.UtcNow,
                ContextSnapshotId = Guid.NewGuid(),
                GraphSnapshotId = Guid.NewGuid(),
                FindingsSnapshotId = Guid.NewGuid(),
                GoldenManifestId = Guid.NewGuid(),
                DecisionTraceId = Guid.NewGuid(),
                ArtifactBundleId = Guid.NewGuid()
            });
        }
    }
}
