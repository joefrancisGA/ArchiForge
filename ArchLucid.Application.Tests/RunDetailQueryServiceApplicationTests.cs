using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

namespace ArchLucid.Application.Tests;

/// <summary>
/// <see cref="RunDetailQueryService"/> coverage in the Application test assembly (broken manifest flag, trace load guard).
/// </summary>
/// <remarks>
/// ADR 0030 PR A3 (2026-04-24): the legacy <c>ICoordinatorDecisionTraceRepository</c> dependency was
/// removed; decision traces are now read from <see cref="IDecisionTraceRepository"/> using
/// <see cref="RunRecord.DecisionTraceId"/>.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class RunDetailQueryServiceApplicationTests
{
    private static ScopeContext NewScope() =>
        new() { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() };

    [SkippableFact]
    public async Task GetRunDetailAsync_when_manifest_version_set_but_manifest_missing_sets_HasBrokenManifestReference()
    {
        ScopeContext scope = NewScope();
        Guid runGuid = Guid.Parse("55555555-5555-5555-5555-555555555555");
        string runN = runGuid.ToString("N");

        Mock<IRunRepository> runRepo = new();
        Mock<IScopeContextProvider> scopeProvider = new();
        Mock<IAgentTaskRepository> taskRepo = new();
        Mock<IAgentResultRepository> resultRepo = new();
        Mock<IUnifiedGoldenManifestReader> unifiedReader = new();
        Mock<IDecisionTraceRepository> authorityTraceRepo = new();

        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(scope);

        RunRecord record = new()
        {
            RunId = runGuid,
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ScopeProjectId = scope.ProjectId,
            ProjectId = "p",
            ArchitectureRequestId = "req-x",
            LegacyRunStatus = nameof(ArchitectureRunStatus.Committed),
            CreatedUtc = DateTime.UtcNow,
            CurrentManifestVersion = "v-missing",
        };

        runRepo.Setup(r => r.GetByIdAsync(scope, runGuid, It.IsAny<CancellationToken>())).ReturnsAsync(record);
        taskRepo.Setup(r => r.GetByRunIdAsync(runN, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        resultRepo.Setup(r => r.GetByRunIdAsync(runN, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        unifiedReader
            .Setup(r => r.ReadByRunIdAsync(scope, runGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoldenManifest?)null);

        RunDetailQueryService sut = new(
            runRepo.Object,
            scopeProvider.Object,
            taskRepo.Object,
            resultRepo.Object,
            unifiedReader.Object,
            authorityTraceRepo.Object,
            new Mock<ILogger<RunDetailQueryService>>().Object);

        ArchitectureRunDetail? detail = await sut.GetRunDetailAsync(runN);

        detail.Should().NotBeNull();
        detail.Manifest.Should().BeNull();
        detail.HasBrokenManifestReference.Should().BeTrue();
        authorityTraceRepo.Verify(
            t => t.GetByIdAsync(It.IsAny<ScopeContext>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
    public async Task GetRunDetailAsync_when_manifest_resolved_and_authority_trace_present_loads_trace()
    {
        ScopeContext scope = NewScope();
        Guid runGuid = Guid.Parse("66666666-6666-6666-6666-666666666666");
        Guid traceId = Guid.Parse("66666666-6666-6666-6666-66666666aaaa");
        string runN = runGuid.ToString("N");

        Mock<IRunRepository> runRepo = new();
        Mock<IScopeContextProvider> scopeProvider = new();
        Mock<IAgentTaskRepository> taskRepo = new();
        Mock<IAgentResultRepository> resultRepo = new();
        Mock<IUnifiedGoldenManifestReader> unifiedReader = new();
        Mock<IDecisionTraceRepository> authorityTraceRepo = new();

        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(scope);

        RunRecord record = new()
        {
            RunId = runGuid,
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ScopeProjectId = scope.ProjectId,
            ProjectId = "p",
            ArchitectureRequestId = "req-y",
            LegacyRunStatus = nameof(ArchitectureRunStatus.Committed),
            CreatedUtc = DateTime.UtcNow,
            CurrentManifestVersion = "v1",
            DecisionTraceId = traceId,
        };

        GoldenManifest manifest = new()
        {
            RunId = runN,
            SystemName = "S",
            Services = [],
            Datastores = [],
            Relationships = [],
            Governance = new ManifestGovernance(),
            Metadata = new ManifestMetadata { ManifestVersion = "v1" },
        };

        DecisionTrace authorityTrace = RunEventTrace.From(new RunEventTracePayload
        {
            TraceId = traceId.ToString("N"), RunId = runN, EventType = "Commit", EventDescription = "authority commit",
        });

        runRepo.Setup(r => r.GetByIdAsync(scope, runGuid, It.IsAny<CancellationToken>())).ReturnsAsync(record);
        taskRepo.Setup(r => r.GetByRunIdAsync(runN, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        resultRepo.Setup(r => r.GetByRunIdAsync(runN, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        unifiedReader
            .Setup(r => r.ReadByRunIdAsync(scope, runGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);
        authorityTraceRepo
            .Setup(r => r.GetByIdAsync(scope, traceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorityTrace);

        RunDetailQueryService sut = new(
            runRepo.Object,
            scopeProvider.Object,
            taskRepo.Object,
            resultRepo.Object,
            unifiedReader.Object,
            authorityTraceRepo.Object,
            new Mock<ILogger<RunDetailQueryService>>().Object);

        ArchitectureRunDetail? detail = await sut.GetRunDetailAsync(runN);

        detail.Should().NotBeNull();
        detail.HasBrokenManifestReference.Should().BeFalse();
        detail.DecisionTraces.Should().HaveCount(1);
    }
}
