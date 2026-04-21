using ArchLucid.Application.Explanation;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Explanation;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Queries;

using FluentAssertions;

using Moq;

namespace ArchLucid.Application.Tests.Explanation;

[Trait("Suite", "Core")]
public sealed class FindingEvidenceChainServiceTests
{
    [Fact]
    public async Task BuildAsync_WhenRunIdNotGuid_ReturnsNull()
    {
        FindingEvidenceChainService sut = CreateSut(out _, out _, out _);

        FindingEvidenceChainResponse? chain = await sut.BuildAsync("not-a-run-id", "f1");

        chain.Should().BeNull();
    }

    [Fact]
    public async Task BuildAsync_WhenAuthorityDetailMissing_ReturnsNull()
    {
        FindingEvidenceChainService sut = CreateSut(out Mock<IAuthorityQueryService> authority, out _, out _);
        authority.Setup(a => a.GetRunDetailAsync(It.IsAny<ScopeContext>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RunDetailDto?)null);

        FindingEvidenceChainResponse? chain = await sut.BuildAsync(Guid.NewGuid().ToString("N"), "f1");

        chain.Should().BeNull();
    }

    [Fact]
    public async Task BuildAsync_WhenFindingMissing_ReturnsNull()
    {
        Guid runGuid = Guid.Parse("22222222-2222-2222-2222-222222222222");
        RunDetailDto dto = MinimalDetail(runGuid, findings: [NewFinding("other-id")]);
        FindingEvidenceChainService sut = CreateSut(out Mock<IAuthorityQueryService> authority, out _, out _);
        authority.Setup(a => a.GetRunDetailAsync(It.IsAny<ScopeContext>(), runGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        FindingEvidenceChainResponse? chain = await sut.BuildAsync(runGuid.ToString("N"), "missing");

        chain.Should().BeNull();
    }

    [Fact]
    public async Task BuildAsync_WhenHappyPath_ReturnsPointersAndDistinctTraceIds()
    {
        Guid runGuid = Guid.Parse("33333333-3333-3333-3333-333333333333");
        RunDetailDto dto = MinimalDetail(runGuid, findings: [NewFinding("hit-id", "g1", "g2")]);
        dto.Run.CurrentManifestVersion = "v7";
        dto.Run.FindingsSnapshotId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        dto.Run.ContextSnapshotId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        dto.Run.GraphSnapshotId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        dto.Run.DecisionTraceId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        dto.Run.GoldenManifestId = Guid.Parse("88888888-8888-8888-8888-888888888888");

        FindingEvidenceChainService sut = CreateSut(out Mock<IAuthorityQueryService> authority, out _, out Mock<IAgentExecutionTraceRepository> traces);
        authority.Setup(a => a.GetRunDetailAsync(It.IsAny<ScopeContext>(), runGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        traces.Setup(t => t.GetByRunIdAsync(runGuid.ToString("N"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new AgentExecutionTrace { TraceId = "t-shared", RunId = runGuid.ToString("N") },
                new AgentExecutionTrace { TraceId = "t-shared", RunId = runGuid.ToString("N") },
                new AgentExecutionTrace { TraceId = "t-b", RunId = runGuid.ToString("N") },
            ]);

        FindingEvidenceChainResponse? chain = await sut.BuildAsync(runGuid.ToString("N"), "hit-id");

        chain.Should().NotBeNull();
        chain.RunId.Should().Be(runGuid.ToString("N"));
        chain.FindingId.Should().Be("hit-id");
        chain.ManifestVersion.Should().Be("v7");
        chain.FindingsSnapshotId.Should().Be(dto.Run.FindingsSnapshotId);
        chain.ContextSnapshotId.Should().Be(dto.Run.ContextSnapshotId);
        chain.GraphSnapshotId.Should().Be(dto.Run.GraphSnapshotId);
        chain.DecisionTraceId.Should().Be(dto.Run.DecisionTraceId);
        chain.GoldenManifestId.Should().Be(dto.Run.GoldenManifestId);
        chain.RelatedGraphNodeIds.Should().Equal("g1", "g2");
        chain.AgentExecutionTraceIds.Should().Equal("t-shared", "t-b");
    }

    private static FindingEvidenceChainService CreateSut(
        out Mock<IAuthorityQueryService> authority,
        out Mock<IScopeContextProvider> scope,
        out Mock<IAgentExecutionTraceRepository> traces)
    {
        authority = new Mock<IAuthorityQueryService>();
        scope = new Mock<IScopeContextProvider>();
        traces = new Mock<IAgentExecutionTraceRepository>();
        scope.Setup(s => s.GetCurrentScope()).Returns(new ScopeContext { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() });

        return new FindingEvidenceChainService(authority.Object, scope.Object, traces.Object);
    }

    private static RunDetailDto MinimalDetail(Guid runGuid, IReadOnlyList<Finding> findings)
    {
        RunRecord run = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ScopeProjectId = Guid.NewGuid(),
            RunId = runGuid,
            ProjectId = "default",
            CreatedUtc = DateTime.UtcNow,
        };

        return new RunDetailDto
        {
            Run = run,
            FindingsSnapshot = new FindingsSnapshot { Findings = findings.ToList() },
        };
    }

    private static Finding NewFinding(string findingId, params string[] related)
    {
        return new Finding
        {
            FindingId = findingId,
            FindingType = "TestFinding",
            Category = "Test",
            EngineType = "Unit",
            Severity = FindingSeverity.Info,
            Title = "t",
            Rationale = "r",
            RelatedNodeIds = related.ToList(),
        };
    }
}
