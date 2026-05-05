using ArchLucid.Application.Decisions;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.Requests;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

namespace ArchLucid.Application.Tests;

/// <summary>
/// <see cref="DefaultAgentEvaluationService"/> returns no evaluations and validates inputs.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class DefaultAgentEvaluationServiceTests
{
    [SkippableFact]
    public async Task EvaluateAsync_returns_empty_list()
    {
        Mock<ILogger<DefaultAgentEvaluationService>> logger = new();
        DefaultAgentEvaluationService sut = new(logger.Object);
        ArchitectureRequest request = new()
        {
            RequestId = "r1",
            SystemName = "S",
            Environment = "prod",
            CloudProvider = CloudProvider.Azure,
            Description = "d",
        };

        AgentEvidencePackage evidence = new() { RunId = "run-1", RequestId = "r1" };

        IReadOnlyList<AgentEvaluation> evaluations = await sut.EvaluateAsync(
            "run-1",
            request,
            evidence,
            [],
            [],
            CancellationToken.None);

        evaluations.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task EvaluateAsync_throws_when_run_id_invalid()
    {
        DefaultAgentEvaluationService sut = new(new Mock<ILogger<DefaultAgentEvaluationService>>().Object);
        ArchitectureRequest request = new()
        {
            RequestId = "r1",
            SystemName = "S",
            Environment = "prod",
            CloudProvider = CloudProvider.Azure,
            Description = "d",
        };

        AgentEvidencePackage evidence = new() { RunId = "run-1", RequestId = "r1" };

        Func<Task> act = () => sut.EvaluateAsync(" ", request, evidence, [], [], CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [SkippableFact]
    public async Task EvaluateAsync_throws_when_cancelled()
    {
        DefaultAgentEvaluationService sut = new(new Mock<ILogger<DefaultAgentEvaluationService>>().Object);
        ArchitectureRequest request = new()
        {
            RequestId = "r1",
            SystemName = "S",
            Environment = "prod",
            CloudProvider = CloudProvider.Azure,
            Description = "d",
        };

        AgentEvidencePackage evidence = new() { RunId = "run-1", RequestId = "r1" };
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        Func<Task> act = () => sut.EvaluateAsync("run-1", request, evidence, [], [], cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [SkippableFact]
    public async Task EvaluateAsync_throws_when_request_null()
    {
        DefaultAgentEvaluationService sut = new(new Mock<ILogger<DefaultAgentEvaluationService>>().Object);
        AgentEvidencePackage evidence = new() { RunId = "run-1", RequestId = "r1" };

        Func<Task> act = () => sut.EvaluateAsync("run-1", null!, evidence, [], [], CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [SkippableFact]
    public async Task EvaluateAsync_throws_when_evidence_null()
    {
        DefaultAgentEvaluationService sut = new(new Mock<ILogger<DefaultAgentEvaluationService>>().Object);
        ArchitectureRequest request = new()
        {
            RequestId = "r1",
            SystemName = "S",
            Environment = "prod",
            CloudProvider = CloudProvider.Azure,
            Description = "d",
        };

        Func<Task> act = () => sut.EvaluateAsync("run-1", request, null!, [], [], CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
