using ArchiForge.ContextIngestion.Models;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;
using ArchiForge.Coordinator.Services;
using ArchiForge.DecisionEngine.Services;
using ArchiForge.DecisionEngine.Validation;
using ArchiForge.Persistence.Models;
using ArchiForge.Persistence.Orchestration;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ArchiForge.AgentRuntime.Tests;

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

        AgentResultParser parser = new AgentResultParser();
        NoOpTraceRecorder traceRecorder = new NoOpTraceRecorder();

        TopologyAgentHandler topologyHandler = new TopologyAgentHandler(
            new StubAgentCompletionClient(topologyJson),
            parser,
            traceRecorder);

        ComplianceAgentHandler complianceHandler = new ComplianceAgentHandler(
            new StubAgentCompletionClient(complianceJson),
            parser,
            traceRecorder);

        CostAgentHandler costHandler = new CostAgentHandler();

        CriticAgentHandler criticHandler = new CriticAgentHandler(
            new StubAgentCompletionClient(criticJson),
            parser,
            traceRecorder);

        RealAgentExecutor executor = new RealAgentExecutor(
        [
            topologyHandler,
            complianceHandler,
            costHandler,
            criticHandler
        ]);

        ArchitectureRequest request = new ArchitectureRequest
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

        CoordinatorService coordinator = new CoordinatorService(new FakeAuthorityRunOrchestratorForRuntimeTests());
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
        {
            task.RunId = "RUN-001";
        }

        AgentEvidencePackage evidence = new AgentEvidencePackage
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

        IReadOnlyList<AgentResult> results = await executor.ExecuteAsync("RUN-001", request, evidence, coordination.Tasks);

        SchemaValidationService validationService = new SchemaValidationService(
            NullLogger<SchemaValidationService>.Instance,
            Options.Create(new SchemaValidationOptions()));

        DecisionEngineService engine = new DecisionEngineService(validationService);
        DecisionMergeResult merge = engine.MergeResults(
            runId: "RUN-001",
            request: request,
            manifestVersion: "v1",
            results: results,
            evaluations: [],
            decisionNodes: [],
            parentManifestVersion: null);

        merge.Success.Should().BeTrue();
        merge.Manifest.Services.Should().Contain(s => s.ServiceName == "rag-api");
        merge.Manifest.Services.Should().Contain(s => s.ServiceName == "rag-search");
        merge.Manifest.Datastores.Should().Contain(d => d.DatastoreName == "rag-metadata");
        merge.Manifest.Governance.RequiredControls.Should().Contain("Managed Identity");
        merge.Manifest.Governance.RequiredControls.Should().Contain("Private Endpoints");
        merge.Manifest.Governance.RequiredControls.Should().Contain("Diagnostic Logging");
    }

    private sealed class FakeAuthorityRunOrchestratorForRuntimeTests : IAuthorityRunOrchestrator
    {
        public Task<RunRecord> ExecuteAsync(ContextIngestionRequest request, CancellationToken ct)
        {
            _ = ct;
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
    }
}
