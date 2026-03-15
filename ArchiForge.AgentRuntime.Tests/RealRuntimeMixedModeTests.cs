using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;
using ArchiForge.Coordinator.Services;
using ArchiForge.DecisionEngine.Services;
using FluentAssertions;
using Xunit;

namespace ArchiForge.AgentRuntime.Tests;

public sealed class RealRuntimeMixedModeTests
{
    [Fact]
    public async Task RealTopologyAndCompliance_WithDeterministicCost_ShouldProduceManifest()
    {
        var topologyJson = """
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

        var complianceJson = """
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

        var parser = new AgentResultParser();

        var topologyHandler = new TopologyAgentHandler(
            new StubAgentCompletionClient(topologyJson),
            parser);

        var complianceHandler = new ComplianceAgentHandler(
            new StubAgentCompletionClient(complianceJson),
            parser);

        var costHandler = new CostAgentHandler();

        var executor = new RealAgentExecutor(new IAgentHandler[]
        {
            topologyHandler,
            complianceHandler,
            costHandler
        });

        var request = new ArchitectureRequest
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

        var coordinator = new CoordinatorService();
        var coordination = coordinator.CreateRun(request);

        // Force known IDs used in stub payloads
        var topologyTask = coordination.Tasks.Single(t => t.AgentType == AgentType.Topology);
        topologyTask.TaskId = "TASK-TOPO-001";

        var complianceTask = coordination.Tasks.Single(t => t.AgentType == AgentType.Compliance);
        complianceTask.TaskId = "TASK-COMP-001";

        var costTask = coordination.Tasks.Single(t => t.AgentType == AgentType.Cost);
        costTask.TaskId = "TASK-COST-001";

        foreach (var task in coordination.Tasks)
        {
            task.RunId = "RUN-001";
        }

        var results = await executor.ExecuteAsync("RUN-001", request, coordination.Tasks);

        var engine = new DecisionEngineService();
        var merge = engine.MergeResults("RUN-001", request, "v1", results);

        merge.Success.Should().BeTrue();
        merge.Manifest.Services.Should().Contain(s => s.ServiceName == "rag-api");
        merge.Manifest.Services.Should().Contain(s => s.ServiceName == "rag-search");
        merge.Manifest.Datastores.Should().Contain(d => d.DatastoreName == "rag-metadata");
        merge.Manifest.Governance.RequiredControls.Should().Contain("Managed Identity");
        merge.Manifest.Governance.RequiredControls.Should().Contain("Private Endpoints");
    }
}
