using System.Diagnostics;

using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;
using ArchiForge.Core.Configuration;
using ArchiForge.Core.Diagnostics;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ArchiForge.AgentRuntime.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class RealAgentExecutorTests
{
    private sealed class StubPromptMonitor(AgentPromptCatalogOptions value) : IOptionsMonitor<AgentPromptCatalogOptions>
    {
        public AgentPromptCatalogOptions CurrentValue { get; } = value;

        public AgentPromptCatalogOptions Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<AgentPromptCatalogOptions, string?> listener) => null;
    }

    private static RealAgentExecutor CreateSut(params IAgentHandler[] handlers) =>
        new(
            handlers,
            NullLogger<RealAgentExecutor>.Instance,
            new StubPromptMonitor(new AgentPromptCatalogOptions()));

    [Fact]
    public void Constructor_when_duplicate_agent_types_throws()
    {
        IAgentHandler[] handlers =
        [
            new StubAgentHandler(AgentType.Topology),
            new StubAgentHandler(AgentType.Topology),
        ];

        Action act = () => _ = CreateSut(handlers);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteAsync_orders_tasks_by_agent_type_and_aggregates_results()
    {
        List<AgentType> observed = [];
        IAgentHandler topology = new OrderingStubHandler(AgentType.Topology, observed);
        IAgentHandler compliance = new OrderingStubHandler(AgentType.Compliance, observed);
        RealAgentExecutor sut = CreateSut(topology, compliance);
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

        // <see cref="RealAgentExecutor"/> orders by <see cref="AgentTypeKeys"/> (lexicographic on dispatch keys).
        observed.Should().Equal(AgentType.Compliance, AgentType.Topology);
        results.Should().HaveCount(2);
        results[0].AgentType.Should().Be(AgentType.Compliance);
        results[1].AgentType.Should().Be(AgentType.Topology);
    }

    [Fact]
    public async Task ExecuteAsync_when_handler_missing_throws()
    {
        RealAgentExecutor sut = CreateSut();
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
            .WithMessage("*cost*");
    }

    [Fact]
    public async Task ExecuteAsync_records_one_activity_per_task_with_agent_tags()
    {
        List<Activity> completed = [];

        using ActivityListener listener = new()
        {
            ShouldListenTo = s => s.Name == ArchiForgeInstrumentation.AgentHandler.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = completed.Add,
        };

        ActivitySource.AddActivityListener(listener);

        try
        {
            IAgentHandler topology = new StubAgentHandler(AgentType.Topology);
            IAgentHandler compliance = new StubAgentHandler(AgentType.Compliance);
            RealAgentExecutor sut = CreateSut(topology, compliance);
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

            await sut.ExecuteAsync(runId, request, evidence, [taskZ, taskC], CancellationToken.None);
        }
        finally
        {
            listener.Dispose();
        }

        completed.Should().HaveCount(2);
        completed.Should().OnlyContain(a => a.OperationName == "archiforge.agent.handle");
        completed[0].GetTagItem("archiforge.agent.type").Should().Be(AgentTypeKeys.Compliance);
        completed[1].GetTagItem("archiforge.agent.type").Should().Be(AgentTypeKeys.Topology);
    }

    private sealed class StubAgentHandler(AgentType agentType) : IAgentHandler
    {
        public AgentType AgentType => agentType;

        public string AgentTypeKey => AgentTypeKeys.FromEnum(agentType);

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

        public string AgentTypeKey => AgentTypeKeys.FromEnum(agentType);

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
