using ArchLucid.AgentRuntime.Prompts;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;

using FluentAssertions;

namespace ArchLucid.AgentRuntime.Tests;

/// <summary>
/// Tests for Topology Agent Handler.
/// </summary>

[Trait("Suite", "Core")]
public sealed class TopologyAgentHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnParsedTopologyAgentResult()
    {
        string json = """
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
                            "findingId": "FIND-001",
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

        StubAgentCompletionClient completionClient = new(json);
        AgentResultParser parser = new();
        NoOpTraceRecorder traceRecorder = new();
        IAgentSystemPromptCatalog catalog = AgentPromptCatalogTestFactory.Create();
        TopologyAgentHandler handler = new(completionClient, parser, traceRecorder, catalog);

        ArchitectureRequest request = new()
        {
            RequestId = "REQ-001",
            SystemName = "EnterpriseRag",
            Description = "Design a secure Azure RAG system.",
            Environment = "prod",
            CloudProvider = CloudProvider.Azure
        };

        AgentTask task = new()
        {
            TaskId = "TASK-TOPO-001",
            RunId = "RUN-001",
            AgentType = AgentType.Topology,
            Objective = "Produce a topology proposal."
        };

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

        AgentResult result = await handler.ExecuteAsync("RUN-001", request, evidence, task);

        result.AgentType.Should().Be(AgentType.Topology);
        result.RunId.Should().Be("RUN-001");
        result.TaskId.Should().Be("TASK-TOPO-001");
        result.ProposedChanges.Should().NotBeNull();
        result.ProposedChanges!.AddedServices.Should().Contain(s => s.ServiceName == "rag-api");
        result.ProposedChanges.AddedServices.Should().Contain(s => s.ServiceName == "rag-search");
        result.ProposedChanges.AddedDatastores.Should().Contain(d => d.DatastoreName == "rag-metadata");
    }
}
