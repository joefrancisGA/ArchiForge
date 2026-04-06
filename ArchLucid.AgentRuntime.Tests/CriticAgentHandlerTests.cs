using ArchLucid.AgentRuntime.Prompts;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;

using FluentAssertions;

namespace ArchLucid.AgentRuntime.Tests;

/// <summary>
/// Tests for Critic Agent Handler.
/// </summary>

[Trait("Suite", "Core")]
public sealed class CriticAgentHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnParsedCriticAgentResult()
    {
        string json = """
                      {
                        "resultId": "RES-CRITIC-001",
                        "taskId": "TASK-CRITIC-001",
                        "runId": "RUN-001",
                        "agentType": "Critic",
                        "claims": [
                          "The architecture should explicitly address observability.",
                          "Secret management should not remain implicit."
                        ],
                        "evidenceRefs": [
                          "critic-checklist",
                          "request"
                        ],
                        "confidence": 0.84,
                        "findings": [
                          {
                            "findingId": "FIND-CRITIC-001",
                            "sourceAgent": "Critic",
                            "severity": "Medium",
                            "category": "Critic",
                            "message": "ObservabilityUnderSpecified",
                            "evidenceRefs": [ "critic-checklist" ]
                          },
                          {
                            "findingId": "FIND-CRITIC-002",
                            "sourceAgent": "Critic",
                            "severity": "Medium",
                            "category": "Critic",
                            "message": "SecretManagementUnderSpecified",
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
                            "Operational observability should be made explicit before production rollout."
                          ]
                        },
                        "createdUtc": "2026-03-15T14:10:00Z"
                      }
                      """;

        StubAgentCompletionClient completionClient = new(json);
        AgentResultParser parser = new();
        NoOpTraceRecorder traceRecorder = new();
        IAgentSystemPromptCatalog catalog = AgentPromptCatalogTestFactory.Create();
        CriticAgentHandler handler = new(completionClient, parser, traceRecorder, catalog);

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

        AgentTask task = new()
        {
            TaskId = "TASK-CRITIC-001",
            RunId = "RUN-001",
            AgentType = AgentType.Critic,
            Objective = "Critique the architecture direction."
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

        result.AgentType.Should().Be(AgentType.Critic);
        result.RunId.Should().Be("RUN-001");
        result.TaskId.Should().Be("TASK-CRITIC-001");
        result.Findings.Should().Contain(f => f.Message == "ObservabilityUnderSpecified");
        result.Findings.Should().Contain(f => f.Message == "SecretManagementUnderSpecified");
        result.ProposedChanges.Should().NotBeNull();
        result.ProposedChanges!.RequiredControls.Should().Contain("Diagnostic Logging");
    }
}
