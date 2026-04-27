using ArchLucid.AgentSimulator.Services;
using ArchLucid.Contracts.Abstractions.Agents;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;

using FluentAssertions;

namespace ArchLucid.AgentRuntime.Tests;

/// <summary>
///     Tests for <see cref="SimulatorExecutionTraceRecordingExecutor" />.
/// </summary>
[Trait("Category", "Unit")]
public sealed class SimulatorExecutionTraceRecordingExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_InvokesRecorderOncePerAgentResult()
    {
        const string runId = "run-test-001";
        ArchitectureRequest request = new()
        {
            RequestId = "REQ-T",
            SystemName = "Sys",
            Description = "Minimum length description for tests here.",
            Environment = "prod",
            CloudProvider = CloudProvider.Azure
        };

        AgentEvidencePackage evidence = CreateMinimalEvidence(runId, request);
        List<AgentTask> tasks = CreateFourTasks(runId);

        SpyAgentExecutionTraceRecorder spy = new();
        DeterministicAgentSimulator inner = new();
        SimulatorExecutionTraceRecordingExecutor sut = new(inner, spy);

        IReadOnlyList<AgentResult> results = await sut.ExecuteAsync(runId, request, evidence, tasks);

        results.Should().HaveCount(4);
        spy.Calls.Should().HaveCount(4);
        spy.Calls.Select(c => c.TaskId).Should().BeEquivalentTo(tasks.Select(t => t.TaskId));
        spy.Calls.Should().OnlyContain(c => c.RunId == runId);
        spy.Calls.Should().OnlyContain(c =>
            c.ModelDeploymentName == AgentExecutionTraceModelMetadata.SimulatorDeploymentName
            && c.ModelVersion == AgentExecutionTraceModelMetadata.SimulatorModelVersion);
        spy.Calls.Should().OnlyContain(c => c.IsSimulatorExecution);
    }

    private static AgentEvidencePackage CreateMinimalEvidence(string runId, ArchitectureRequest request)
    {
        return new AgentEvidencePackage
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
    }

    private static List<AgentTask> CreateFourTasks(string runId)
    {
        return
        [
            new AgentTask
            {
                TaskId = "task-topology",
                RunId = runId,
                AgentType = AgentType.Topology,
                Objective = "Propose topology."
            },
            new AgentTask
            {
                TaskId = "task-cost", RunId = runId, AgentType = AgentType.Cost, Objective = "Estimate cost."
            },
            new AgentTask
            {
                TaskId = "task-compliance",
                RunId = runId,
                AgentType = AgentType.Compliance,
                Objective = "Check compliance."
            },
            new AgentTask
            {
                TaskId = "task-critic", RunId = runId, AgentType = AgentType.Critic, Objective = "Critique design."
            }
        ];
    }
}
