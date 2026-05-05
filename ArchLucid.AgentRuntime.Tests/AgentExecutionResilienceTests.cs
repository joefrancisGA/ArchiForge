using System.Diagnostics;

using ArchLucid.Contracts.Abstractions.Agents;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Scoping;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Polly.Timeout;

namespace ArchLucid.AgentRuntime.Tests;

/// <summary>
///     Validates bulkhead (concurrency gate) and per-handler Polly timeout wiring on <see cref="RealAgentExecutor" />.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class AgentExecutionResilienceTests
{
    private static ArchitectureRequest MinimalRequest()
    {
        return new ArchitectureRequest
        {
            RequestId = "r1", Description = "1234567890ab", SystemName = "S", Environment = "prod"
        };
    }

    [SkippableFact]
    public async Task Bulkhead_max_concurrency_one_serializes_slow_handlers()
    {
        IOptions<AgentExecutionResilienceOptions> ro = Options.Create(
            new AgentExecutionResilienceOptions { MaxConcurrentHandlers = 1, PerHandlerTimeoutSeconds = 0 });

        RealAgentExecutor sut = new(
            [
                new SlowStubHandler(AgentType.Topology, 120),
                new SlowStubHandler(AgentType.Compliance, 120)
            ],
            NullLogger<RealAgentExecutor>.Instance,
            new StubPromptMonitor(new AgentPromptCatalogOptions()),
            new FixedScopeProvider(),
            new AgentHandlerConcurrencyGate(ro),
            ro);

        ArchitectureRequest request = MinimalRequest();
        AgentEvidencePackage evidence = new();
        string runId = Guid.NewGuid().ToString("N");
        AgentTask taskTopology = new() { TaskId = "tz", RunId = runId, AgentType = AgentType.Topology };
        AgentTask taskCompliance = new() { TaskId = "tc", RunId = runId, AgentType = AgentType.Compliance };

        Stopwatch sw = Stopwatch.StartNew();

        await sut.ExecuteAsync(runId, request, evidence, [taskTopology, taskCompliance], CancellationToken.None);

        sw.Elapsed.Should().BeGreaterThan(TimeSpan.FromMilliseconds(200));
    }

    [SkippableFact]
    public async Task Bulkhead_unlimited_allows_parallel_slow_handlers()
    {
        IOptions<AgentExecutionResilienceOptions> ro = Options.Create(
            new AgentExecutionResilienceOptions { MaxConcurrentHandlers = 0, PerHandlerTimeoutSeconds = 0 });

        RealAgentExecutor sut = new(
            [
                new SlowStubHandler(AgentType.Topology, 150),
                new SlowStubHandler(AgentType.Compliance, 150)
            ],
            NullLogger<RealAgentExecutor>.Instance,
            new StubPromptMonitor(new AgentPromptCatalogOptions()),
            new FixedScopeProvider(),
            new AgentHandlerConcurrencyGate(ro),
            ro);

        ArchitectureRequest request = MinimalRequest();
        AgentEvidencePackage evidence = new();
        string runId = Guid.NewGuid().ToString("N");
        AgentTask taskTopology = new() { TaskId = "tz", RunId = runId, AgentType = AgentType.Topology };
        AgentTask taskCompliance = new() { TaskId = "tc", RunId = runId, AgentType = AgentType.Compliance };

        Stopwatch sw = Stopwatch.StartNew();

        await sut.ExecuteAsync(runId, request, evidence, [taskTopology, taskCompliance], CancellationToken.None);

        // Two ~150ms handlers in parallel: allow thread-pool / full-solution parallel test load (not serial ~300ms+).
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(5000));
    }

    [SkippableFact]
    public async Task Per_handler_timeout_aborts_hanging_handler()
    {
        IOptions<AgentExecutionResilienceOptions> ro = Options.Create(
            new AgentExecutionResilienceOptions { MaxConcurrentHandlers = 0, PerHandlerTimeoutSeconds = 1 });

        RealAgentExecutor sut = new(
            [new HangingHandler(AgentType.Topology)],
            NullLogger<RealAgentExecutor>.Instance,
            new StubPromptMonitor(new AgentPromptCatalogOptions()),
            new FixedScopeProvider(),
            new AgentHandlerConcurrencyGate(ro),
            ro);

        ArchitectureRequest request = MinimalRequest();
        AgentEvidencePackage evidence = new();
        string runId = Guid.NewGuid().ToString("N");
        AgentTask task = new() { TaskId = "t1", RunId = runId, AgentType = AgentType.Topology };

        Func<Task> act = async () =>
            await sut.ExecuteAsync(runId, request, evidence, [task], CancellationToken.None);

        await act.Should().ThrowAsync<TimeoutRejectedException>();
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

    private sealed class FixedScopeProvider : IScopeContextProvider
    {
        public ScopeContext GetCurrentScope()
        {
            return new ScopeContext
            {
                TenantId = ScopeIds.DefaultTenant,
                WorkspaceId = ScopeIds.DefaultWorkspace,
                ProjectId = ScopeIds.DefaultProject
            };
        }
    }

    private sealed class SlowStubHandler(AgentType agentType, int delayMs) : IAgentHandler
    {
        public AgentType AgentType => agentType;

        public string AgentTypeKey => AgentTypeKeys.FromEnum(agentType);

        public async Task<AgentResult> ExecuteAsync(
            string runId,
            ArchitectureRequest request,
            AgentEvidencePackage evidence,
            AgentTask task,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(delayMs, cancellationToken);

            return new AgentResult
            {
                RunId = runId,
                TaskId = task.TaskId,
                AgentType = agentType,
                Claims = [],
                EvidenceRefs = []
            };
        }
    }

    private sealed class HangingHandler(AgentType agentType) : IAgentHandler
    {
        public AgentType AgentType => agentType;

        public string AgentTypeKey => AgentTypeKeys.FromEnum(agentType);

        public async Task<AgentResult> ExecuteAsync(
            string runId,
            ArchitectureRequest request,
            AgentEvidencePackage evidence,
            AgentTask task,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);

            return new AgentResult();
        }
    }
}
