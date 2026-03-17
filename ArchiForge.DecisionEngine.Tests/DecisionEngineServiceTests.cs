using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Decisions;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Requests;
using ArchiForge.DecisionEngine.Services;
using Xunit;

namespace ArchiForge.DecisionEngine.Tests;

public sealed class DecisionEngineServiceTests
{
    [Fact]
    public void MergeResults_Should_CreateManifest_When_ValidResultsProvided()
    {
        var request = new ArchitectureRequest
        {
            RequestId = "REQ-001",
            SystemName = "TestSystem",
            Description = "Design a secure Azure system."
        };

        var topology = new AgentResult
        {
            ResultId = "RES-1",
            TaskId = "TASK-1",
            RunId = "RUN-1",
            AgentType = AgentType.Topology,
            Claims = ["Add API service"],
            EvidenceRefs = ["request"],
            Confidence = 0.90,
            ProposedChanges = new ManifestDeltaProposal
            {
                ProposalId = "PROP-1",
                SourceAgent = AgentType.Topology,
                AddedServices =
                [
                    new ManifestService
                    {
                        ServiceId = "svc-1",
                        ServiceName = "api",
                        ServiceType = ServiceType.Api,
                        RuntimePlatform = RuntimePlatform.AppService
                    }
                ]
            }
        };

        var compliance = new AgentResult
        {
            ResultId = "RES-2",
            TaskId = "TASK-2",
            RunId = "RUN-1",
            AgentType = AgentType.Compliance,
            Claims = ["Managed Identity required"],
            EvidenceRefs = ["policy-pack"],
            Confidence = 0.95,
            ProposedChanges = new ManifestDeltaProposal
            {
                ProposalId = "PROP-2",
                SourceAgent = AgentType.Compliance,
                RequiredControls = ["Managed Identity"]
            }
        };

        var service = new DecisionEngineService();

        var result = service.MergeResults(
            "1",
            request,
            "v1",
            [topology, compliance],
            evaluations: [],
            decisionNodes: [],
            parentManifestVersion: null);

        Assert.True(result.Success);
        Assert.Single(result.Manifest.Services);
        Assert.Contains("Managed Identity", result.Manifest.Governance.RequiredControls);
        Assert.Contains(
            result.Manifest.Services[0].RequiredControls,
            c => c.Equals("Managed Identity", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MergeResults_Should_Fail_When_ResultIsMalformed()
    {
        var request = new ArchitectureRequest
        {
            RequestId = "REQ-001",
            SystemName = "TestSystem",
            Description = "Design a secure Azure system."
        };

        var malformed = new AgentResult
        {
            ResultId = "",
            TaskId = "",
            RunId = "",
            AgentType = AgentType.Topology,
            Claims = [],
            EvidenceRefs = [],
            Confidence = 1.2
        };

        var service = new DecisionEngineService();

        var result = service.MergeResults(
            "1",
            request,
            "v1",
            [malformed],
            evaluations: [],
            decisionNodes: [],
            parentManifestVersion: null);

        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }
}