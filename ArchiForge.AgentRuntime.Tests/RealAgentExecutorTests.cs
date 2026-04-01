using FluentAssertions;

using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;

using Microsoft.Extensions.Logging.Abstractions;

namespace ArchiForge.AgentRuntime.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class RealAgentExecutorTests
{
    [Fact]
    public void Constructor_when_duplicate_agent_types_throws()
    {
        IAgentHandler[] handlers =
        [
            new StubAgentHandler(AgentType.Topology),
            new StubAgentHandler(AgentType.Topology),
        ];

        Action act = () => _ = new RealAgentExecutor(handlers, NullLogger<RealAgentExecutor>.Instance);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteAsync_orders_tasks_by_agent_type_and_aggregates_results()
    {
        List<AgentType> observed = [];
        IAgentHandler topology = new OrderingStubHandler(AgentType.Topology, observed);
        IAgentHandler compliance = new OrderingStubHandler(AgentType.Compliance, observed);
        RealAgentExecutor sut = new([topology, compliance], NullLogger<RealAgentExecutor>.Instance);
        ArchitectureRequest request = new()
        {
            RequestId = "r1",
            Description = "1234567890ab",
            SystemName = "S",
            Environment = "prod",
        };
        AgentEvidencePackage evidence = new();
        string runId = Guid.NewGuid().ToString("N");
        AgentTask taskZ = new()
        {
            TaskId = "tz",
            RunId = runId,
            AgentType = AgentType.Topology,
        };
        AgentTask taskC = new()
        {
            TaskId = "tc",
            RunId = runId,
            AgentType = AgentType.Compliance,
        };

        IReadOnlyList<AgentResult> results =
            await sut.ExecuteAsync(runId, request, evidence, [taskZ, taskC], CancellationToken.None);

        // <see cref="RealAgentExecutor"/> orders by enum value (not alphabetical name).
        observed.Should().Equal(AgentType.Topology, AgentType.Compliance);
        results.Should().HaveCount(2);
        results[0].AgentType.Should().Be(AgentType.Topology);
    }

    [Fact]
    public async Task ExecuteAsync_when_handler_missing_throws()
    {
        RealAgentExecutor sut = new(
            Array.Empty<IAgentHandler>(),
            NullLogger<RealAgentExecutor>.Instance);
        ArchitectureRequest request = new()
        {
            RequestId = "r1",
            Description = "1234567890ab",
            SystemName = "S",
        };
        AgentTask task = new()
        {
            TaskId = "t",
            RunId = "run",
            AgentType = AgentType.Cost,
        };

        Func<Task> act = async () =>
            await sut.ExecuteAsync("run", request, new AgentEvidencePackage(), [task], CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cost*");
    }

    private sealed class StubAgentHandler(AgentType agentType) : IAgentHandler
    {
        public AgentType AgentType => agentType;

        public Task<AgentResult> ExecuteAsync(
            string runId,
            ArchitectureRequest request,
            AgentEvidencePackage evidence,
            AgentTask task,
            CancellationToken cancellationToken = default)
        {
            _ = runId;
            _ = request;
            _ = evidence;
            _ = task;
            _ = cancellationToken;

            return Task.FromResult(new AgentResult());
        }
    }

    private sealed class OrderingStubHandler(AgentType agentType, List<AgentType> observed) : IAgentHandler
    {
        public AgentType AgentType => agentType;

        public Task<AgentResult> ExecuteAsync(
            string runId,
            ArchitectureRequest request,
            AgentEvidencePackage evidence,
            AgentTask task,
            CancellationToken cancellationToken = default)
        {
            observed.Add(agentType);

            return Task.FromResult(
                new AgentResult
                {
                    RunId = runId,
                    TaskId = task.TaskId,
                    AgentType = agentType,
                    Claims = [],
                    EvidenceRefs = [],
                });
        }
    }
}
