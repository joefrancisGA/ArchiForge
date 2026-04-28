using ArchLucid.AgentRuntime.Prompts;
using ArchLucid.ContextIngestion.Models;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;
using ArchLucid.Application.Runs.Coordination;
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
///     Tests for Real Runtime Mixed Mode.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
public sealed class RealRuntimeMixedModeTests
{
    [Fact]
    public async Task RealTopologyAndCompliance_WithDeterministicCost_ShouldProduceManifest()
    {
        string topologyJson = """
                              {
                                "resultId": "RES-TOPO-001",
                                "taskId": "TASK-TOPO-001",
                                "runId": "RUN-001",
                                "agentType": "Topology",
                                "claims": [
                                  "Use App Service for the API.",
                                  "Use Azure AI Search for retrieval."
                                ],
                                "evidenceRefs": [
                                  "request",
                                  "catalog:azure-ai-search"
                                ],
                                "confidence": 0.91,
                                "findings": [
                                  {
                                    "findingId": "FIND-TOPO-001",
                                    "sourceAgent": "Topology",
                                    "severity": "Info",
                                    "category": "Topology",
                                    "message": "Managed Azure services fit the MVP.",
                                    "evidenceRefs": [ "request" ]
                                  }
                                ],
                                "proposedChanges": {
                                  "proposalId": "PROP-TOPO-001",
                                  "sourceAgent": "Topology",
                                  "addedServices": [
                                    {
                                      "serviceId": "svc-api",
                                      "serviceName": "rag-api",
                                      "serviceType": "Api",
                                      "runtimePlatform": "AppService",
                                      "purpose": "Primary API"
                                    },
                                    {
                                      "serviceId": "svc-search",
                                      "serviceName": "rag-search",
                                      "serviceType": "SearchService",
                                      "runtimePlatform": "AzureAiSearch",
                                      "purpose": "Retrieval layer"
                                    }
                                  ],
                                  "addedDatastores": [
                                    {
                                      "datastoreId": "ds-metadata",
                                      "datastoreName": "rag-metadata",
                                      "datastoreType": "Sql",
                                      "runtimePlatform": "SqlServer",
                                      "purpose": "Metadata storage",
                                      "privateEndpointRequired": false,
                                      "encryptionAtRestRequired": true
                                    }
                                  ],
                                  "addedRelationships": [
                                    {
                                      "relationshipId": "REL-001",
                                      "sourceId": "svc-api",
                                      "targetId": "svc-search",
                                      "relationshipType": "Calls",
                                      "description": "API queries search"
                                    }
                                  ],
                                  "requiredControls": [],
                                  "warnings": [
                                    "Simple topology selected."
                                  ]
                                },
                                "createdUtc": "2026-03-15T14:00:00Z"
                              }
                              """;

        string complianceJson = """
                                {
                                  "resultId": "RES-COMP-001",
                                  "taskId": "TASK-COMP-001",
                                  "runId": "RUN-001",
                                  "agentType": "Compliance",
                                  "claims": [
                                    "Managed identity is required.",
                                    "Private endpoints are required."
                                  ],
                                  "evidenceRefs": [
                                    "policy-pack:enterprise-default"
                                  ],
                                  "confidence": 0.95,
                                  "findings": [
                                    {
                                      "findingId": "FIND-COMP-001",
                                      "sourceAgent": "Compliance",
                                      "severity": "High",
                                      "category": "Compliance",
                                      "message": "ManagedIdentityRequired",
                                      "evidenceRefs": [ "policy-pack:enterprise-default" ]
                                    }
                                  ],
                                  "proposedChanges": {
                                    "proposalId": "PROP-COMP-001",
                                    "sourceAgent": "Compliance",
                                    "addedServices": [],
                                    "addedDatastores": [],
                                    "addedRelationships": [],
                                    "requiredControls": [
                                      "Managed Identity",
                                      "Private Endpoints",
                                      "Key Vault"
                                    ],
                                    "warnings": [
                                      "Public access should require exception review."
                                    ]
                                  },
                                  "createdUtc": "2026-03-15T14:00:00Z"
                                }
                                """;

        string criticJson = """
                            {
                              "resultId": "RES-CRITIC-001",
                              "taskId": "TASK-CRITIC-001",
                              "runId": "RUN-001",
                              "agentType": "Critic",
                              "claims": [
                                "Observability should be explicit."
                              ],
                              "evidenceRefs": [
                                "critic-checklist"
                              ],
                              "confidence": 0.83,
                              "findings": [
                                {
                                  "findingId": "FIND-CRITIC-001",
                                  "sourceAgent": "Critic",
                                  "severity": "Medium",
                                  "category": "Critic",
                                  "message": "ObservabilityUnderSpecified",
                                  "evidenceRefs": [ "critic-checklist" ]
                                }
                              ],
                              "proposedChanges": {
                                "proposalId": "PROP-CRITIC-001",
                                "sourceAgent": "Critic",
                                "addedServices": [],
                                "addedDatastores": [],
                                "addedRelationships": [],
                                "requiredControls": [
                                  "Diagnostic Logging"
                                ],
                                "warnings": [
                                  "Add operational diagnostics before production."
                                ]
                              },
                              "createdUtc": "2026-03-15T14:10:00Z"
                            }
                            """;

        AgentResultParser parser = new();
        NoOpTraceRecorder traceRecorder = new();

        IAgentSystemPromptCatalog promptCatalog = AgentPromptCatalogTestFactory.Create();

        Mock<IAuditService> audit = new();
        audit.Setup(a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(
            new ScopeContext { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() });

        IOptionsMonitor<AgentSchemaRemediationOptions> schemaRemediation =
            AgentSchemaRemediationOptionsMonitorTestFactory.Create();

        TopologyAgentHandler topologyHandler = new(
            new StubAgentCompletionClient(topologyJson),
            parser,
            traceRecorder,
            promptCatalog,
            audit.Object,
            scopeProvider.Object,
            schemaRemediation);

        ComplianceAgentHandler complianceHandler = new(
            new StubAgentCompletionClient(complianceJson),
            parser,
            traceRecorder,
            promptCatalog,
            audit.Object,
            scopeProvider.Object,
            schemaRemediation);

        CostAgentHandler costHandler = new();

        CriticAgentHandler criticHandler = new(
            new StubAgentCompletionClient(criticJson),
            parser,
            traceRecorder,
            promptCatalog,
            audit.Object,
            scopeProvider.Object,
            schemaRemediation);

        IOptions<AgentExecutionResilienceOptions> resilience = Options.Create(
            new AgentExecutionResilienceOptions { MaxConcurrentHandlers = 0, PerHandlerTimeoutSeconds = 0 });

        RealAgentExecutor executor = new(
            [
                topologyHandler,
                complianceHandler,
                costHandler,
                criticHandler
            ],
            NullLogger<RealAgentExecutor>.Instance,
            new MixedModePromptMonitor(new AgentPromptCatalogOptions()),
            new FixedScopeProviderForMixedMode(),
            new AgentHandlerConcurrencyGate(resilience),
            resilience);

        ArchitectureRequest request = new()
        {
            RequestId = "REQ-001",
            SystemName = "EnterpriseRag",
            Description = "Design a secure Azure RAG system.",
            Environment = "prod",
            CloudProvider = CloudProvider.Azure,
            Constraints =
            [
                "Private endpoints required",
                "Use managed identity"
            ],
            RequiredCapabilities =
            [
                "Azure AI Search",
                "SQL",
                "Managed Identity",
                "Private Networking"
            ]
        };

        Mock<IRunRepository> runRepo = new();
        runRepo.Setup(r => r.GetByIdAsync(It.IsAny<ScopeContext>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RunRecord?)null);

        ArchitectureRunAuthorityCoordination coordinator = new(
            new FakeAuthorityRunOrchestratorForRuntimeTests(),
            runRepo.Object,
            scopeProvider.Object,
            NullLogger<ArchitectureRunAuthorityCoordination>.Instance);
        CoordinationResult coordination = await coordinator.CreateRunAsync(request);

        // Force known IDs used in stub payloads
        AgentTask topologyTask = coordination.Tasks.Single(t => t.AgentType == AgentType.Topology);
        topologyTask.TaskId = "TASK-TOPO-001";

        AgentTask complianceTask = coordination.Tasks.Single(t => t.AgentType == AgentType.Compliance);
        complianceTask.TaskId = "TASK-COMP-001";

        AgentTask costTask = coordination.Tasks.Single(t => t.AgentType == AgentType.Cost);
        costTask.TaskId = "TASK-COST-001";

        AgentTask criticTask = coordination.Tasks.Single(t => t.AgentType == AgentType.Critic);
        criticTask.TaskId = "TASK-CRITIC-001";
        criticTask.RunId = "RUN-001";

        foreach (AgentTask task in coordination.Tasks)

            task.RunId = "RUN-001";


        AgentEvidencePackage evidence = new()
        {
            RunId = "RUN-001",
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
            await executor.ExecuteAsync("RUN-001", request, evidence, coordination.Tasks);

        SchemaValidationService validationService = new(
            NullLogger<SchemaValidationService>.Instance,
            Options.Create(new SchemaValidationOptions()));

        DecisionEngineService engine = new(validationService);
        DecisionMergeResult merge = engine.MergeResults(
            "RUN-001",
            request,
            "v1",
            results,
            [],
            []);

        merge.Success.Should().BeTrue();
        merge.Manifest.Services.Should().Contain(s => s.ServiceName == "rag-api");
        merge.Manifest.Services.Should().Contain(s => s.ServiceName == "rag-search");
        merge.Manifest.Datastores.Should().Contain(d => d.DatastoreName == "rag-metadata");
        merge.Manifest.Governance.RequiredControls.Should().Contain("Managed Identity");
        merge.Manifest.Governance.RequiredControls.Should().Contain("Private Endpoints");
        merge.Manifest.Governance.RequiredControls.Should().Contain("Diagnostic Logging");
    }

    private sealed class FixedScopeProviderForMixedMode : IScopeContextProvider
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

    private sealed class FakeAuthorityRunOrchestratorForRuntimeTests : IAuthorityRunOrchestrator
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
