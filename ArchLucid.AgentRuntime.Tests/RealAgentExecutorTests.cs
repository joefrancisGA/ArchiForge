using System.Diagnostics;

using ArchLucid.Contracts.Abstractions.Agents;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class RealAgentExecutorTests
{
    private static IOptions<AgentExecutionResilienceOptions> UnlimitedResilienceOptions()
    {
        return Options.Create(
            new AgentExecutionResilienceOptions { MaxConcurrentHandlers = 0, PerHandlerTimeoutSeconds = 0 });
    }

    private static RealAgentExecutor CreateSut(params IAgentHandler[] handlers)
    {
        IOptions<AgentExecutionResilienceOptions> ro = UnlimitedResilienceOptions();

        return new RealAgentExecutor(
            handlers,
            NullLogger<RealAgentExecutor>.Instance,
            new StubPromptMonitor(new AgentPromptCatalogOptions()),
            new FixedScopeProvider(
                new ScopeContext
                {
                    TenantId = ScopeIds.DefaultTenant,
                    WorkspaceId = ScopeIds.DefaultWorkspace,
                    ProjectId = ScopeIds.DefaultProject
                }),
            new AgentHandlerConcurrencyGate(ro),
            ro);
    }

    [SkippableFact]
    public void Constructor_when_duplicate_agent_types_throws()
    {
        IAgentHandler[] handlers =
        [
            new StubAgentHandler(AgentType.Topology),
            new StubAgentHandler(AgentType.Topology)
        ];

        Action act = () => _ = CreateSut(handlers);

        act.Should().Throw<ArgumentException>();
    }

    [SkippableFact]
    public async Task ExecuteAsync_orders_results_by_agent_type_regardless_of_completion_order()
    {
        List<AgentType> observed = [];
        IAgentHandler topology = new OrderingStubHandler(AgentType.Topology, observed);
        IAgentHandler compliance = new OrderingStubHandler(AgentType.Compliance, observed);
        RealAgentExecutor sut = CreateSut(topology, compliance);
        ArchitectureRequest request = new()
        {
            RequestId = "r1", Description = "1234567890ab", SystemName = "S", Environment = "prod"
        };
        AgentEvidencePackage evidence = new();
        string runId = Guid.NewGuid().ToString("N");
        AgentTask taskZ = new() { TaskId = "tz", RunId = runId, AgentType = AgentType.Topology };
        AgentTask taskC = new() { TaskId = "tc", RunId = runId, AgentType = AgentType.Compliance };

        IReadOnlyList<AgentResult> results =
            await sut.ExecuteAsync(runId, request, evidence, [taskZ, taskC], CancellationToken.None);

        // Result list stays ordered by dispatch key (Compliance before Topology); handlers may finish in any order.
        observed.Should().BeEquivalentTo([AgentType.Compliance, AgentType.Topology]);
        results.Should().HaveCount(2);
        results[0].AgentType.Should().Be(AgentType.Compliance);
        results[1].AgentType.Should().Be(AgentType.Topology);
    }

    [SkippableFact]
    public async Task ExecuteAsync_when_handler_missing_throws()
    {
        RealAgentExecutor sut = CreateSut();
        ArchitectureRequest request = new() { RequestId = "r1", Description = "1234567890ab", SystemName = "S" };
        AgentTask task = new() { TaskId = "t", RunId = "run", AgentType = AgentType.Cost };

        Func<Task> act = async () =>
            await sut.ExecuteAsync("run", request, new AgentEvidencePackage(), [task], CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cost*");
    }

    [SkippableFact]
    public async Task ExecuteAsync_records_one_activity_per_task_with_agent_tags()
    {
        List<Activity> completed = [];

        using ActivityListener listener = new();
        listener.ShouldListenTo = s => s.Name == ArchLucidInstrumentation.AgentHandler.Name;
        listener.Sample = (ref _) => ActivitySamplingResult.AllData;
        listener.ActivityStopped = completed.Add;

        ActivitySource.AddActivityListener(listener);

        IAgentHandler topology = new StubAgentHandler(AgentType.Topology);
        IAgentHandler compliance = new StubAgentHandler(AgentType.Compliance);
        RealAgentExecutor sut = CreateSut(topology, compliance);
        ArchitectureRequest request = new()
        {
            RequestId = "r1", Description = "1234567890ab", SystemName = "S", Environment = "prod"
        };
        AgentEvidencePackage evidence = new();
        string runId = Guid.NewGuid().ToString("N");
        AgentTask taskZ = new() { TaskId = "tz", RunId = runId, AgentType = AgentType.Topology };
        AgentTask taskC = new() { TaskId = "tc", RunId = runId, AgentType = AgentType.Compliance };

        await sut.ExecuteAsync(runId, request, evidence, [taskZ, taskC], CancellationToken.None);

        completed.Should().HaveCount(2);
        completed.Should().OnlyContain(a => a.OperationName == "archlucid.agent.handle");

        string[] types = completed
            .Select(a => (string)a.GetTagItem("archlucid.agent.type")!)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        types.Should().Equal(AgentTypeKeys.Compliance, AgentTypeKeys.Topology);
    }

    [SkippableFact]
    public async Task ExecuteAsync_runs_multiple_handlers_concurrently_so_topology_can_unblock_compliance()
    {
        // Dispatch-key order is Compliance then Topology. Compliance blocks until Topology runs; sequential execution would deadlock.
        using SemaphoreSlim complianceMayContinue = new(0, 1);

        IAgentHandler compliance = new DeadlockAwareComplianceHandler(complianceMayContinue);
        IAgentHandler topology = new SignalingTopologyHandler(complianceMayContinue);

        RealAgentExecutor sut = CreateSut(topology, compliance);
        ArchitectureRequest request = new()
        {
            RequestId = "r1", Description = "1234567890ab", SystemName = "S", Environment = "prod"
        };
        AgentEvidencePackage evidence = new();
        string runId = Guid.NewGuid().ToString("N");
        AgentTask taskTopology = new() { TaskId = "tz", RunId = runId, AgentType = AgentType.Topology };
        AgentTask taskCompliance = new() { TaskId = "tc", RunId = runId, AgentType = AgentType.Compliance };

        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));

        Func<Task> act = async () =>
            await sut.ExecuteAsync(runId, request, evidence, [taskTopology, taskCompliance], timeout.Token);

        await act.Should().NotThrowAsync();
    }

    private sealed class StubPromptMonitor(AgentPromptCatalogOptions value) : IOptionsMonitor<AgentPromptCatalogOptions>
    {
        public AgentPromptCatalogOptions CurrentValue
        {
            get;
        } = value;

        public AgentPromptCatalogOptions Get(string? name)
        {
            return CurrentValue;
        }

        public IDisposable? OnChange(Action<AgentPromptCatalogOptions, string?> listener)
        {
            return null;
        }
    }

    private sealed class FixedScopeProvider(ScopeContext scope) : IScopeContextProvider
    {
        public ScopeContext GetCurrentScope()
        {
            return scope;
        }
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
                    EvidenceRefs = []
                });
        }
    }

    private sealed class DeadlockAwareComplianceHandler(SemaphoreSlim complianceMayContinue) : IAgentHandler
    {
        public AgentType AgentType => AgentType.Compliance;

        public string AgentTypeKey => AgentTypeKeys.Compliance;

        public async Task<AgentResult> ExecuteAsync(
            string runId,
            ArchitectureRequest request,
            AgentEvidencePackage evidence,
            AgentTask task,
            CancellationToken cancellationToken = default)
        {
            await complianceMayContinue.WaitAsync(cancellationToken);

            return new AgentResult
            {
                RunId = runId,
                TaskId = task.TaskId,
                AgentType = AgentType.Compliance,
                Claims = [],
                EvidenceRefs = []
            };
        }
    }

    private sealed class SignalingTopologyHandler(SemaphoreSlim complianceMayContinue) : IAgentHandler
    {
        public AgentType AgentType => AgentType.Topology;

        public string AgentTypeKey => AgentTypeKeys.Topology;

        public Task<AgentResult> ExecuteAsync(
            string runId,
            ArchitectureRequest request,
            AgentEvidencePackage evidence,
            AgentTask task,
            CancellationToken cancellationToken = default)
        {
            complianceMayContinue.Release();

            return Task.FromResult(
                new AgentResult
                {
                    RunId = runId,
                    TaskId = task.TaskId,
                    AgentType = AgentType.Topology,
                    Claims = [],
                    EvidenceRefs = []
                });
        }
    }
}
