using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;
using FluentAssertions;
using Xunit;

namespace ArchiForge.AgentRuntime.Tests;

public sealed class ComplianceAgentHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnParsedComplianceAgentResult()
    {
        var json = """
{
  "resultId": "RES-COMP-001",
  "taskId": "TASK-COMP-001",
  "runId": "RUN-001",
  "agentType": "Compliance",
  "claims": [
    "Managed identity is required.",
    "Private endpoints are required for data-bearing services.",
    "Secrets should be stored in Key Vault."
  ],
  "evidenceRefs": [
    "policy-pack:enterprise-default",
    "policy-pack:azure-security-baseline"
  ],
  "confidence": 0.95,
  "findings": [
    {
      "findingId": "FIND-COMP-001",
      "sourceAgent": "Compliance",
      "severity": "High",
      "category": "Compliance",
      "message": "ManagedIdentityRequired",
      "evidenceRefs": [ "policy-pack:azure-security-baseline" ]
    },
    {
      "findingId": "FIND-COMP-002",
      "sourceAgent": "Compliance",
      "severity": "High",
      "category": "Compliance",
      "message": "PrivateNetworkingRequired",
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
      "Key Vault",
      "Encryption At Rest"
    ],
    "warnings": [
      "Public network exposure should require explicit exception review."
    ]
  },
  "createdUtc": "2026-03-15T14:05:00Z"
}
""";

        var completionClient = new StubAgentCompletionClient(json);
        var parser = new AgentResultParser();
        var handler = new ComplianceAgentHandler(completionClient, parser);

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
                "Use managed identity",
                "Encryption at rest required"
            ],
            RequiredCapabilities =
            [
                "Azure AI Search",
                "SQL",
                "Managed Identity",
                "Private Networking"
            ]
        };

        var task = new AgentTask
        {
            TaskId = "TASK-COMP-001",
            RunId = "RUN-001",
            AgentType = AgentType.Compliance,
            Objective = "Produce a compliance proposal."
        };

        var result = await handler.ExecuteAsync("RUN-001", request, task);

        result.AgentType.Should().Be(AgentType.Compliance);
        result.RunId.Should().Be("RUN-001");
        result.TaskId.Should().Be("TASK-COMP-001");
        result.ProposedChanges.Should().NotBeNull();
        result.ProposedChanges!.RequiredControls.Should().Contain("Managed Identity");
        result.ProposedChanges.RequiredControls.Should().Contain("Private Endpoints");
        result.ProposedChanges.RequiredControls.Should().Contain("Key Vault");
        result.Findings.Should().Contain(f => f.Message == "ManagedIdentityRequired");
        result.Findings.Should().Contain(f => f.Message == "PrivateNetworkingRequired");
    }
}
