using ArchLucid.Application.Runs.Finalization;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Integration;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.TestSupport;

using FluentAssertions;

using Moq;

namespace ArchLucid.Application.Tests.Runs.Finalization;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ManifestFinalizationServiceTests
{
    [SkippableFact]
    public async Task FinalizeAsync_throws_when_request_is_null()
    {
        ManifestFinalizationService sut = CreateSut();

        Func<Task> act = async () => await sut.FinalizeAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [SkippableFact]
    public async Task FinalizeAsync_legacy_path_throws_InvalidOperation_when_findings_snapshot_mismatch()
    {
        Guid runId = Guid.NewGuid();
        Guid expectedFindings = Guid.NewGuid();

        ScopeContext scope = new() { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() };

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(scope);

        RunRecord header = new()
        {
            RunId = runId,
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ScopeProjectId = scope.ProjectId,
            LegacyRunStatus = nameof(ArchitectureRunStatus.ReadyForCommit),
            FindingsSnapshotId = Guid.NewGuid()
        };

        Mock<IRunRepository> runs = new();
        runs.Setup(r => r.GetByIdAsync(scope, runId, It.IsAny<CancellationToken>())).ReturnsAsync(header);

        ManifestFinalizationService sut = CreateSut(
            scopeProvider: scopeProvider.Object,
            runRepository: runs.Object);

        ManifestFinalizationRequest request = CreateMinimalRequest(runId, expectedFindings);

        Func<Task> act = async () => await sut.FinalizeAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Findings*");
    }

    [SkippableFact]
    public async Task FinalizeAsync_legacy_path_throws_when_findings_generation_not_sealed()
    {
        Guid runId = Guid.NewGuid();
        Guid findingsId = Guid.NewGuid();

        ScopeContext scope = new() { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() };

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(scope);

        RunRecord header = new()
        {
            RunId = runId,
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ScopeProjectId = scope.ProjectId,
            LegacyRunStatus = nameof(ArchitectureRunStatus.ReadyForCommit),
            FindingsSnapshotId = findingsId
        };

        Mock<IRunRepository> runs = new();
        runs.Setup(r => r.GetByIdAsync(scope, runId, It.IsAny<CancellationToken>())).ReturnsAsync(header);

        Mock<IFindingsSnapshotRepository> findings = new();
        findings.Setup(f => f.GetByIdAsync(findingsId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new FindingsSnapshot { FindingsSnapshotId = findingsId, GenerationStatus = FindingsSnapshotGenerationStatus.Generating, Findings = [] });

        ManifestFinalizationService sut = CreateSut(
            scopeProvider: scopeProvider.Object,
            runRepository: runs.Object,
            findingsSnapshotRepository: findings.Object);

        ManifestFinalizationRequest request = CreateMinimalRequest(runId, findingsId);

        Func<Task> act = async () => await sut.FinalizeAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not eligible*");
    }

    [SkippableFact]
    public async Task FinalizeAsync_legacy_path_throws_ConflictException_when_run_status_invalid()
    {
        Guid runId = Guid.NewGuid();
        Guid findingsId = Guid.NewGuid();

        ScopeContext scope = new() { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() };

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(scope);

        RunRecord header = new()
        {
            RunId = runId,
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ScopeProjectId = scope.ProjectId,
            LegacyRunStatus = nameof(ArchitectureRunStatus.Failed),
            FindingsSnapshotId = findingsId
        };

        Mock<IRunRepository> runs = new();
        runs.Setup(r => r.GetByIdAsync(scope, runId, It.IsAny<CancellationToken>())).ReturnsAsync(header);

        ManifestFinalizationService sut = CreateSut(
            scopeProvider: scopeProvider.Object,
            runRepository: runs.Object);

        ManifestFinalizationRequest request = CreateMinimalRequest(runId, findingsId);

        Func<Task> act = async () => await sut.FinalizeAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [SkippableFact]
    public async Task FinalizeAsync_legacy_path_calls_UpdateAsync_EnqueueAsync_and_LogAsync_on_success()
    {
        Guid runId = Guid.NewGuid();
        Guid findingsId = Guid.NewGuid();
        Guid traceId = Guid.NewGuid();

        ScopeContext scope = new() { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() };

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(scope);

        RunRecord header = new()
        {
            RunId = runId,
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ScopeProjectId = scope.ProjectId,
            ProjectId = "proj",
            LegacyRunStatus = nameof(ArchitectureRunStatus.ReadyForCommit),
            FindingsSnapshotId = findingsId
        };

        Mock<IRunRepository> runs = new();
        runs.Setup(r => r.GetByIdAsync(scope, runId, It.IsAny<CancellationToken>())).ReturnsAsync(header);

        Mock<IDecisionTraceRepository> traces = new();
        traces.Setup(t => t.SaveAsync(It.IsAny<DecisionTrace>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IGoldenManifestRepository> golden = new();
        ManifestDocument persisted = CreateMinimalManifest(runId, findingsId, traceId);
        golden.Setup(g => g.SaveAsync(
                It.IsAny<GoldenManifest>(),
                scope,
                It.IsAny<SaveContractsManifestOptions>(),
                It.IsAny<IManifestHashService>(),
                It.IsAny<CancellationToken>(),
                null,
                null,
                It.IsAny<ManifestDocument>()))
            .ReturnsAsync(persisted);

        Mock<IAuditService> audit = new();
        audit.Setup(a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        Mock<IIntegrationEventOutboxRepository> outbox = new();
        outbox.Setup(o => o.EnqueueAsync(
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ManifestFinalizationService sut = CreateSut(
            scopeProvider: scopeProvider.Object,
            runRepository: runs.Object,
            decisionTraceRepository: traces.Object,
            goldenManifestRepository: golden.Object,
            auditService: audit.Object,
            integrationEventOutbox: outbox.Object);

        ManifestFinalizationRequest request = CreateMinimalRequest(runId, findingsId, traceId);

        ManifestFinalizationResult result = await sut.FinalizeAsync(request, CancellationToken.None);

        result.WasIdempotentReturn.Should().BeFalse();
        result.ManifestId.Should().Be(persisted.ManifestId);
        runs.Verify(
            r => r.UpdateAsync(It.Is<RunRecord>(h => h.LegacyRunStatus == nameof(ArchitectureRunStatus.Committed)),
                It.IsAny<CancellationToken>(),
                null,
                null),
            Times.Once);
        audit.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e => e.EventType == AuditEventTypes.ManifestFinalized),
                It.IsAny<CancellationToken>()),
            Times.Once);
        outbox.Verify(
            o => o.EnqueueAsync(
                runId,
                IntegrationEventTypes.ManifestFinalizedV1,
                It.IsAny<string>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                scope.TenantId,
                scope.WorkspaceId,
                scope.ProjectId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [SkippableFact]
    public async Task FinalizeAsync_legacy_returns_idempotent_when_run_already_committed()
    {
        Guid runId = Guid.NewGuid();
        Guid manifestId = Guid.NewGuid();

        ScopeContext scope = new() { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() };

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(scope);

        RunRecord header = new()
        {
            RunId = runId,
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ScopeProjectId = scope.ProjectId,
            LegacyRunStatus = nameof(ArchitectureRunStatus.Committed),
            GoldenManifestId = manifestId,
            CurrentManifestVersion = "v2",
            FindingsSnapshotId = Guid.NewGuid()
        };

        Mock<IRunRepository> runs = new();
        runs.Setup(r => r.GetByIdAsync(scope, runId, It.IsAny<CancellationToken>())).ReturnsAsync(header);

        ManifestFinalizationService sut = CreateSut(scopeProvider: scopeProvider.Object, runRepository: runs.Object);

        ManifestFinalizationRequest request = CreateMinimalRequest(runId, header.FindingsSnapshotId!.Value);

        ManifestFinalizationResult result = await sut.FinalizeAsync(request, CancellationToken.None);

        result.WasIdempotentReturn.Should().BeTrue();
        result.ManifestId.Should().Be(manifestId);
        result.ManifestVersion.Should().Be("v2");
        result.PersistedManifest.Should().BeNull();
    }

    private static ManifestFinalizationRequest CreateMinimalRequest(
        Guid runId,
        Guid findingsId,
        Guid? decisionTraceId = null)
    {
        Guid tid = decisionTraceId ?? Guid.NewGuid();
        DecisionTrace trace = RuleAuditTrace.From(
            new RuleAuditTracePayload
            {
                DecisionTraceId = tid,
                RunId = runId,
                TenantId = Guid.NewGuid(),
                WorkspaceId = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                CreatedUtc = DateTime.UtcNow,
                RuleSetId = "rs",
                RuleSetVersion = "1",
                RuleSetHash = "h",
                AppliedRuleIds = [],
                AcceptedFindingIds = [],
                RejectedFindingIds = [],
                Notes = [],
            });

        ManifestDocument model = CreateMinimalManifest(runId, findingsId, tid);

        return new ManifestFinalizationRequest
        {
            RunId = runId,
            ExpectedFindingsSnapshotId = findingsId,
            ActorUserId = "u1",
            ActorUserName = "User One",
            ManifestModel = model,
            Contract = new GoldenManifest
            {
                RunId = runId.ToString("N"),
                SystemName = "Sys",
                Services = [],
                Datastores = [],
                Relationships = [],
                Governance = new ManifestGovernance(),
                Metadata = new ManifestMetadata { ManifestVersion = "v1" },
            },
            Keying = new SaveContractsManifestOptions
            {
                ManifestId = model.ManifestId,
                RunId = model.RunId,
                ContextSnapshotId = model.ContextSnapshotId,
                GraphSnapshotId = model.GraphSnapshotId,
                FindingsSnapshotId = model.FindingsSnapshotId,
                DecisionTraceId = tid,
                RuleSetId = model.RuleSetId,
                RuleSetVersion = model.RuleSetVersion,
                RuleSetHash = model.RuleSetHash,
                CreatedUtc = model.CreatedUtc,
            },
            Trace = trace,
        };
    }

    private static ManifestDocument CreateMinimalManifest(Guid runId, Guid findingsId, Guid decisionTraceId)
    {
        return new ManifestDocument
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            ManifestId = Guid.NewGuid(),
            RunId = runId,
            ContextSnapshotId = Guid.NewGuid(),
            GraphSnapshotId = Guid.NewGuid(),
            FindingsSnapshotId = findingsId,
            DecisionTraceId = decisionTraceId,
            CreatedUtc = DateTime.UtcNow,
            ManifestHash = "hash",
            RuleSetId = "rs",
            RuleSetVersion = "1",
            RuleSetHash = "rh",
        };
    }

    private static ManifestFinalizationService CreateSut(
        IScopeContextProvider? scopeProvider = null,
        IRunRepository? runRepository = null,
        IDecisionTraceRepository? decisionTraceRepository = null,
        IGoldenManifestRepository? goldenManifestRepository = null,
        IManifestHashService? manifestHashService = null,
        IAuditService? auditService = null,
        IIntegrationEventOutboxRepository? integrationEventOutbox = null,
        IFindingsSnapshotRepository? findingsSnapshotRepository = null)
    {
        Mock<IScopeContextProvider> scope = new();
        scope.Setup(s => s.GetCurrentScope()).Returns(
            new ScopeContext { TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid() });

        return new ManifestFinalizationService(
            ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
            scopeProvider ?? scope.Object,
            runRepository ?? Mock.Of<IRunRepository>(),
            findingsSnapshotRepository ?? CreateDefaultFindingsSnapshotRepository(),
            decisionTraceRepository ?? Mock.Of<IDecisionTraceRepository>(),
            goldenManifestRepository ?? Mock.Of<IGoldenManifestRepository>(),
            manifestHashService ?? Mock.Of<IManifestHashService>(),
            auditService ?? Mock.Of<IAuditService>(),
            integrationEventOutbox ?? Mock.Of<IIntegrationEventOutboxRepository>());
    }

    private static IFindingsSnapshotRepository CreateDefaultFindingsSnapshotRepository()
    {
        Mock<IFindingsSnapshotRepository> mock = new();
        mock.Setup(f => f.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
                new FindingsSnapshot { FindingsSnapshotId = id, GenerationStatus = FindingsSnapshotGenerationStatus.Complete, Findings = [] });

        return mock.Object;
    }
}
