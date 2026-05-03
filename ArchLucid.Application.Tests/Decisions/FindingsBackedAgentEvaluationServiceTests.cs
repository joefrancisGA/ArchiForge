using ArchLucid.Application.Decisions;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.Findings;
using ArchLucid.Contracts.Requests;

using EvalTypes = ArchLucid.Contracts.Decisions.EvaluationTypes;

using FluentAssertions;

namespace ArchLucid.Application.Tests.Decisions;

/// <summary>
///     <see cref="FindingsBackedAgentEvaluationService"/> maps <see cref="ArchitectureFinding"/> to
///     <see cref="AgentEvaluation"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class FindingsBackedAgentEvaluationServiceTests
{
    private static readonly ArchitectureRequest SampleRequest = new()
    {
        RequestId = "r1",
        SystemName = "S",
        Environment = "prod",
        CloudProvider = CloudProvider.Azure,
        Description = "d"
    };

    private static readonly AgentEvidencePackage SampleEvidence = new()
    {
        RunId = "run-1",
        RequestId = "r1"
    };

    private readonly FindingsBackedAgentEvaluationService _sut = new();

    [SkippableFact]
    public async Task EvaluateAsync_when_no_findings_returns_empty()
    {
        IReadOnlyList<AgentEvaluation> evaluations = await _sut.EvaluateAsync(
            "run-1",
            SampleRequest,
            SampleEvidence,
            [new AgentTask { TaskId = "T-topo", RunId = "run-1", AgentType = AgentType.Topology, Status = AgentTaskStatus.Completed }],
            [new AgentResult { RunId = "run-1", TaskId = "T-topo", AgentType = AgentType.Topology, Confidence = 0.8 }],
            CancellationToken.None);

        evaluations.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task EvaluateAsync_critical_compliance_cautions_topology_task()
    {
        List<AgentTask> tasks =
        [
            new() { TaskId = "T-topo", RunId = "run-1", AgentType = AgentType.Topology, Status = AgentTaskStatus.Completed },
            new() { TaskId = "T-comp", RunId = "run-1", AgentType = AgentType.Compliance, Status = AgentTaskStatus.Completed },
        ];

        List<AgentResult> results =
        [
            new()
            {
                RunId = "run-1",
                TaskId = "T-comp",
                AgentType = AgentType.Compliance,
                Confidence = 0.9,
                Findings =
                [
                    new ArchitectureFinding
                    {
                        SourceAgent = AgentType.Compliance,
                        Severity = FindingSeverity.Critical,
                        Category = "network",
                        Message = "Public storage without private endpoint."
                    }
                ]
            },
        ];

        IReadOnlyList<AgentEvaluation> evaluations = await _sut.EvaluateAsync(
            "run-1",
            SampleRequest,
            SampleEvidence,
            tasks,
            results,
            CancellationToken.None);

        AgentEvaluation evaluation = evaluations.Should().ContainSingle().Subject;
        evaluation.RunId.Should().Be("run-1");
        evaluation.TargetAgentTaskId.Should().Be("T-topo");
        evaluation.EvaluationType.Should().Be(EvalTypes.Caution);
        evaluation.ConfidenceDelta.Should().Be(-0.12);
        evaluation.Rationale.Should().Contain("private");
    }

    [SkippableFact]
    public async Task EvaluateAsync_error_compliance_opposes_with_weaker_delta()
    {
        List<AgentTask> tasks =
        [
            new() { TaskId = "T-topo", RunId = "run-1", AgentType = AgentType.Topology, Status = AgentTaskStatus.Completed },
            new() { TaskId = "T-comp", RunId = "run-1", AgentType = AgentType.Compliance, Status = AgentTaskStatus.Completed },
        ];

        List<AgentResult> results =
        [
            new()
            {
                RunId = "run-1",
                TaskId = "T-comp",
                AgentType = AgentType.Compliance,
                Findings =
                [
                    new ArchitectureFinding
                    {
                        Severity = FindingSeverity.Error,
                        Message = "Gap"
                    }
                ]
            },
        ];

        IReadOnlyList<AgentEvaluation> evaluations = await _sut.EvaluateAsync(
            "run-1",
            SampleRequest,
            SampleEvidence,
            tasks,
            results,
            CancellationToken.None);

        AgentEvaluation opposition = evaluations.Should().ContainSingle().Subject;
        opposition.EvaluationType.Should().Be(EvalTypes.Oppose);
        opposition.ConfidenceDelta.Should().Be(-0.15);
        opposition.TargetAgentTaskId.Should().Be("T-topo");
    }

    [SkippableFact]
    public async Task EvaluateAsync_critical_critic_cautions_topology_task()
    {
        List<AgentTask> tasks =
        [
            new() { TaskId = "T-topo", RunId = "run-1", AgentType = AgentType.Topology, Status = AgentTaskStatus.Completed },
            new() { TaskId = "T-crit", RunId = "run-1", AgentType = AgentType.Critic, Status = AgentTaskStatus.Completed },
        ];

        List<AgentResult> results =
        [
            new()
            {
                RunId = "run-1",
                TaskId = "T-crit",
                AgentType = AgentType.Critic,
                Findings =
                [
                    new ArchitectureFinding { Severity = FindingSeverity.Critical, Message = "Contradiction" }
                ]
            },
        ];

        IReadOnlyList<AgentEvaluation> evaluations = await _sut.EvaluateAsync(
            "run-1",
            SampleRequest,
            SampleEvidence,
            tasks,
            results,
            CancellationToken.None);

        evaluations.Should().ContainSingle();
        AgentEvaluation caution = evaluations[0];
        caution.TargetAgentTaskId.Should().Be("T-topo");
        caution.EvaluationType.Should().Be(EvalTypes.Caution);
        caution.ConfidenceDelta.Should().Be(-0.12);
    }

    [SkippableFact]
    public async Task EvaluateAsync_warning_from_cost_yields_caution()
    {
        List<AgentTask> tasks =
        [
            new() { TaskId = "T-topo", RunId = "run-1", AgentType = AgentType.Topology, Status = AgentTaskStatus.Completed },
            new() { TaskId = "T-cost", RunId = "run-1", AgentType = AgentType.Cost, Status = AgentTaskStatus.Completed },
        ];

        List<AgentResult> results =
        [
            new()
            {
                RunId = "run-1",
                TaskId = "T-cost",
                AgentType = AgentType.Cost,
                Findings = [new ArchitectureFinding { Severity = FindingSeverity.Warning, Message = "Spend risk" }],
            },
        ];

        IReadOnlyList<AgentEvaluation> evaluations = await _sut.EvaluateAsync(
            "run-1",
            SampleRequest,
            SampleEvidence,
            tasks,
            results,
            CancellationToken.None);

        AgentEvaluation caution = evaluations.Should().ContainSingle().Subject;
        caution.EvaluationType.Should().Be(EvalTypes.Caution);
        caution.ConfidenceDelta.Should().Be(-0.10);
        caution.TargetAgentTaskId.Should().Be("T-topo");
    }

    [SkippableFact]
    public async Task EvaluateAsync_info_from_topology_yields_support()
    {
        List<AgentTask> tasks =
        [
            new() { TaskId = "T-topo", RunId = "run-1", AgentType = AgentType.Topology, Status = AgentTaskStatus.Completed },
        ];

        List<AgentResult> results =
        [
            new()
            {
                RunId = "run-1",
                TaskId = "T-topo",
                AgentType = AgentType.Topology,
                Findings = [new ArchitectureFinding { Severity = FindingSeverity.Info, Message = "Alignment note" }],
            },
        ];

        IReadOnlyList<AgentEvaluation> evaluations = await _sut.EvaluateAsync(
            "run-1",
            SampleRequest,
            SampleEvidence,
            tasks,
            results,
            CancellationToken.None);

        AgentEvaluation support = evaluations.Should().ContainSingle().Subject;
        support.EvaluationType.Should().Be(EvalTypes.Support);
        support.ConfidenceDelta.Should().Be(0.05);
    }

    [SkippableFact]
    public async Task EvaluateAsync_info_from_compliance_is_skipped()
    {
        List<AgentTask> tasks =
        [
            new() { TaskId = "T-topo", RunId = "run-1", AgentType = AgentType.Topology, Status = AgentTaskStatus.Completed },
            new() { TaskId = "T-comp", RunId = "run-1", AgentType = AgentType.Compliance, Status = AgentTaskStatus.Completed },
        ];

        List<AgentResult> results =
        [
            new()
            {
                RunId = "run-1",
                TaskId = "T-comp",
                AgentType = AgentType.Compliance,
                Findings = [new ArchitectureFinding { Severity = FindingSeverity.Info, Message = "Note" }],
            },
        ];

        IReadOnlyList<AgentEvaluation> evaluations = await _sut.EvaluateAsync(
            "run-1",
            SampleRequest,
            SampleEvidence,
            tasks,
            results,
            CancellationToken.None);

        evaluations.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task EvaluateAsync_without_topology_task_targets_source_task()
    {
        List<AgentTask> tasks =
        [
            new() { TaskId = "T-comp", RunId = "run-1", AgentType = AgentType.Compliance, Status = AgentTaskStatus.Completed },
        ];

        List<AgentResult> results =
        [
            new()
            {
                RunId = "run-1",
                TaskId = "T-comp",
                AgentType = AgentType.Compliance,
                Findings =
                [
                    new ArchitectureFinding { Severity = FindingSeverity.Critical, Message = "Policy breach" },
                ],
            },
        ];

        IReadOnlyList<AgentEvaluation> evaluations = await _sut.EvaluateAsync(
            "run-1",
            SampleRequest,
            SampleEvidence,
            tasks,
            results,
            CancellationToken.None);

        evaluations.Should().ContainSingle().Subject.TargetAgentTaskId.Should().Be("T-comp");
    }

    [SkippableFact]
    public async Task EvaluateAsync_throws_when_run_id_invalid()
    {
        Func<Task> act = () => _sut.EvaluateAsync(" ", SampleRequest, SampleEvidence, [], [], CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
