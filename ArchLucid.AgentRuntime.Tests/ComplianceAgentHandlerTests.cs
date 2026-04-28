using ArchLucid.AgentRuntime.Prompts;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;

using FluentAssertions;

using Moq;

namespace ArchLucid.AgentRuntime.Tests;

/// <summary>
///     Tests for Compliance Agent Handler.
/// </summary>
[Trait("Suite", "Core")]
public sealed class ComplianceAgentHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnParsedComplianceAgentResult()
    {
        const string json = """
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

        StubAgentCompletionClient completionClient = new(json);
        AgentResultParser parser = new();
        NoOpTraceRecorder traceRecorder = new();
        IAgentSystemPromptCatalog catalog = AgentPromptCatalogTestFactory.Create();
        Mock<IAuditService> audit = new();
        audit.Setup(a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(
            new ScopeContext { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() });

        ComplianceAgentHandler handler = new(
            completionClient,
            parser,
            traceRecorder,
            catalog,
            audit.Object,
            scopeProvider.Object,
            AgentSchemaRemediationOptionsMonitorTestFactory.Create());

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

        AgentTask task = new()
        {
            TaskId = "TASK-COMP-001",
            RunId = "RUN-001",
            AgentType = AgentType.Compliance,
            Objective = "Produce a compliance proposal."
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
