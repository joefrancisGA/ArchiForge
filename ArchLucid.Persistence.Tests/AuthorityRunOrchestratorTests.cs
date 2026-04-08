using ArchLucid.ContextIngestion.Models;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Authority;
using ArchLucid.Core.Integration;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Manifest.Sections;
using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Integration;
using ArchLucid.Persistence.Orchestration;
using ArchLucid.Persistence.Orchestration.Pipeline;
using ArchLucid.Persistence.Coordination.Retrieval;
using ArchLucid.Core.Transactions;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

using System.Data;
using System.Text.Json;

using DecisioningManifestMetadata = ArchLucid.Decisioning.Manifest.Sections.ManifestMetadata;

namespace ArchLucid.Persistence.Tests;

/// <summary>
/// <see cref="AuthorityRunOrchestrator"/> unit tests (commit vs rollback, sync vs queued modes).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class AuthorityRunOrchestratorTests
{
    [Fact]
    public async Task ExecuteAsync_sync_mode_commits_and_enqueues_retrieval()
    {
        ScopeContext scope = new()
        {
            TenantId = Guid.Parse("a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1"),
            WorkspaceId = Guid.Parse("a2a2a2a2-a2a2-a2a2-a2a2-a2a2a2a2a2a2"),
            ProjectId = Guid.Parse("a3a3a3a3-a3a3-a3a3-a3a3-a3a3a3a3a3a3"),
        };

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(x => x.GetCurrentScope()).Returns(scope);

        Mock<IArchLucidUnitOfWork> uow = new();
        uow.SetupGet(x => x.SupportsExternalTransaction).Returns(false);
        uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);

        Mock<IArchLucidUnitOfWorkFactory> uowFactory = new();
        uowFactory.Setup(f => f.CreateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(uow.Object);

        Mock<IRunRepository> runRepo = new();
        runRepo.Setup(x => x.SaveAsync(It.IsAny<RunRecord>(), It.IsAny<CancellationToken>(), null, null))
            .Returns(Task.CompletedTask);
        runRepo.Setup(x => x.UpdateAsync(It.IsAny<RunRecord>(), It.IsAny<CancellationToken>(), null, null))
            .Returns(Task.CompletedTask);

        Guid contextSnapshotId = Guid.NewGuid();
        Guid findingsId = Guid.NewGuid();
        Guid traceId = Guid.NewGuid();
        Guid manifestId = Guid.NewGuid();

        Mock<IAuthorityPipelineStagesExecutor> pipeline = new();
        pipeline
            .Setup(x => x.ExecuteAfterRunPersistedAsync(It.IsAny<AuthorityPipelineContext>(), It.IsAny<CancellationToken>()))
            .Callback<AuthorityPipelineContext, CancellationToken>(
                (ctx, _) =>
                {
                    ctx.ContextSnapshot = new ContextSnapshot
                    {
                        SnapshotId = contextSnapshotId,
                        RunId = ctx.Run.RunId,
                        ProjectId = ctx.Request.ProjectId,
                        CreatedUtc = DateTime.UtcNow,
                    };

                    ctx.FindingsSnapshot = new FindingsSnapshot
                    {
                        FindingsSnapshotId = findingsId,
                        RunId = ctx.Run.RunId,
                        ContextSnapshotId = contextSnapshotId,
                        GraphSnapshotId = Guid.NewGuid(),
                        CreatedUtc = DateTime.UtcNow,
                    };

                    ctx.Trace = RuleAuditTrace.From(new RuleAuditTracePayload
                    {
                        TenantId = ctx.Scope.TenantId,
                        WorkspaceId = ctx.Scope.WorkspaceId,
                        ProjectId = ctx.Scope.ProjectId,
                        DecisionTraceId = traceId,
                        RunId = ctx.Run.RunId,
                        CreatedUtc = DateTime.UtcNow,
                        RuleSetId = "rs",
                        RuleSetVersion = "1",
                        RuleSetHash = "h",
                    });

                    ctx.Manifest = NewMinimalManifest(
                        ctx.Scope,
                        ctx.Run.RunId,
                        contextSnapshotId,
                        Guid.NewGuid(),
                        findingsId,
                        traceId,
                        manifestId);
                })
            .Returns(Task.CompletedTask);

        Mock<IRetrievalIndexingOutboxRepository> retrievalOutbox = new();
        retrievalOutbox.Setup(x => x.EnqueueAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IAuthorityPipelineWorkRepository> workRepo = new();
        Mock<IAsyncAuthorityPipelineModeResolver> modeResolver = new();
        modeResolver
            .Setup(x => x.ShouldQueueContextAndGraphStagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Mock<IAuditService> audit = new();
        audit.Setup(x => x.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IIntegrationEventPublisher> integrationEvents = new();
        integrationEvents
            .Setup(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        integrationEvents
            .Setup(x => x.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IIntegrationEventOutboxRepository> integrationOutbox = new();
        StubIntegrationOutbox(integrationOutbox);
        Mock<IOptionsMonitor<IntegrationEventsOptions>> integrationEventOpts = CreateIntegrationEventsOptionsMonitor(false);

        AuthorityRunOrchestrator sut = new(
            uowFactory.Object,
            scopeProvider.Object,
            audit.Object,
            runRepo.Object,
            pipeline.Object,
            retrievalOutbox.Object,
            workRepo.Object,
            modeResolver.Object,
            integrationEvents.Object,
            integrationOutbox.Object,
            integrationEventOpts.Object,
            NullLogger<AuthorityRunOrchestrator>.Instance);

        ContextIngestionRequest request = new()
        {
            ProjectId = "proj-orchestrator-test",
            Description = "d",
        };

        RunRecord result = await sut.ExecuteAsync(request, CancellationToken.None);

        result.ProjectId.Should().Be(request.ProjectId);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
        workRepo.Verify(
            x => x.EnqueueAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        retrievalOutbox.Verify(
            x => x.EnqueueAsync(result.RunId, scope.TenantId, scope.WorkspaceId, scope.ProjectId, It.IsAny<CancellationToken>()),
            Times.Once);
        integrationOutbox.Verify(
            x => x.EnqueueAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<IDbConnection>(),
                It.IsAny<IDbTransaction>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        integrationEvents.Verify(
            x => x.PublishAsync(
                IntegrationEventTypes.AuthorityRunCompletedV1,
                It.IsAny<ReadOnlyMemory<byte>>(),
                $"{result.RunId:D}:{IntegrationEventTypes.AuthorityRunCompletedV1}",
                It.IsAny<CancellationToken>()),
            Times.Once);
        audit.Verify(
            x => x.LogAsync(
                It.Is<AuditEvent>(e =>
                    e.EventType == AuditEventTypes.RunStarted
                    && e.RunId == result.RunId
                    && JsonDocument.Parse(e.DataJson).RootElement.GetProperty("queued").GetBoolean() == false),
                It.IsAny<CancellationToken>()),
            Times.Once);
        audit.Verify(
            x => x.LogAsync(
                It.Is<AuditEvent>(e => e.EventType == AuditEventTypes.RunCompleted),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_with_transactional_integration_outbox_enqueues_sql_and_skips_immediate_publish()
    {
        ScopeContext scope = new()
        {
            TenantId = Guid.Parse("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1"),
            WorkspaceId = Guid.Parse("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2"),
            ProjectId = Guid.Parse("b3b3b3b3-b3b3-b3b3-b3b3-b3b3b3b3b3b3"),
        };

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(x => x.GetCurrentScope()).Returns(scope);

        Mock<IDbConnection> connection = new();
        Mock<IDbTransaction> transaction = new();

        Mock<IArchLucidUnitOfWork> uow = new();
        uow.SetupGet(x => x.SupportsExternalTransaction).Returns(true);
        uow.SetupGet(x => x.Connection).Returns(connection.Object);
        uow.SetupGet(x => x.Transaction).Returns(transaction.Object);
        uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);

        Mock<IArchLucidUnitOfWorkFactory> uowFactory = new();
        uowFactory.Setup(f => f.CreateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(uow.Object);

        Mock<IRunRepository> runRepo = new();
        runRepo.Setup(x => x.SaveAsync(It.IsAny<RunRecord>(), It.IsAny<CancellationToken>(), null, null))
            .Returns(Task.CompletedTask);
        runRepo.Setup(x => x.UpdateAsync(It.IsAny<RunRecord>(), It.IsAny<CancellationToken>(), null, null))
            .Returns(Task.CompletedTask);

        Guid contextSnapshotId = Guid.NewGuid();
        Guid findingsId = Guid.NewGuid();
        Guid traceId = Guid.NewGuid();
        Guid manifestId = Guid.NewGuid();

        Mock<IAuthorityPipelineStagesExecutor> pipeline = new();
        pipeline
            .Setup(x => x.ExecuteAfterRunPersistedAsync(It.IsAny<AuthorityPipelineContext>(), It.IsAny<CancellationToken>()))
            .Callback<AuthorityPipelineContext, CancellationToken>(
                (ctx, _) =>
                {
                    ctx.ContextSnapshot = new ContextSnapshot
                    {
                        SnapshotId = contextSnapshotId,
                        RunId = ctx.Run.RunId,
                        ProjectId = ctx.Request.ProjectId,
                        CreatedUtc = DateTime.UtcNow,
                    };

                    ctx.FindingsSnapshot = new FindingsSnapshot
                    {
                        FindingsSnapshotId = findingsId,
                        RunId = ctx.Run.RunId,
                        ContextSnapshotId = contextSnapshotId,
                        GraphSnapshotId = Guid.NewGuid(),
                        CreatedUtc = DateTime.UtcNow,
                    };

                    ctx.Trace = RuleAuditTrace.From(new RuleAuditTracePayload
                    {
                        TenantId = ctx.Scope.TenantId,
                        WorkspaceId = ctx.Scope.WorkspaceId,
                        ProjectId = ctx.Scope.ProjectId,
                        DecisionTraceId = traceId,
                        RunId = ctx.Run.RunId,
                        CreatedUtc = DateTime.UtcNow,
                        RuleSetId = "rs",
                        RuleSetVersion = "1",
                        RuleSetHash = "h",
                    });

                    ctx.Manifest = NewMinimalManifest(
                        ctx.Scope,
                        ctx.Run.RunId,
                        contextSnapshotId,
                        Guid.NewGuid(),
                        findingsId,
                        traceId,
                        manifestId);
                })
            .Returns(Task.CompletedTask);

        Mock<IRetrievalIndexingOutboxRepository> retrievalOutbox = new();
        retrievalOutbox.Setup(x => x.EnqueueAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<IDbConnection>(),
                It.IsAny<IDbTransaction>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IAuthorityPipelineWorkRepository> workRepo = new();
        Mock<IAsyncAuthorityPipelineModeResolver> modeResolver = new();
        modeResolver
            .Setup(x => x.ShouldQueueContextAndGraphStagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Mock<IAuditService> audit = new();
        audit.Setup(x => x.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IIntegrationEventPublisher> integrationEvents = new();
        integrationEvents
            .Setup(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        integrationEvents
            .Setup(x => x.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IIntegrationEventOutboxRepository> integrationOutbox = new();
        StubIntegrationOutbox(integrationOutbox);
        Mock<IOptionsMonitor<IntegrationEventsOptions>> integrationEventOpts = CreateIntegrationEventsOptionsMonitor(true);

        AuthorityRunOrchestrator sut = new(
            uowFactory.Object,
            scopeProvider.Object,
            audit.Object,
            runRepo.Object,
            pipeline.Object,
            retrievalOutbox.Object,
            workRepo.Object,
            modeResolver.Object,
            integrationEvents.Object,
            integrationOutbox.Object,
            integrationEventOpts.Object,
            NullLogger<AuthorityRunOrchestrator>.Instance);

        ContextIngestionRequest request = new()
        {
            ProjectId = "proj-orchestrator-outbox",
            Description = "d",
        };

        RunRecord result = await sut.ExecuteAsync(request, CancellationToken.None);

        result.ProjectId.Should().Be(request.ProjectId);
        integrationOutbox.Verify(
            x => x.EnqueueAsync(
                result.RunId,
                IntegrationEventTypes.AuthorityRunCompletedV1,
                $"{result.RunId:D}:{IntegrationEventTypes.AuthorityRunCompletedV1}",
                It.IsAny<ReadOnlyMemory<byte>>(),
                scope.TenantId,
                scope.WorkspaceId,
                scope.ProjectId,
                connection.Object,
                transaction.Object,
                It.IsAny<CancellationToken>()),
            Times.Once);
        integrationEvents.Verify(
            x => x.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_queue_mode_enqueues_work_and_commits_without_running_stages()
    {
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
        };

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(x => x.GetCurrentScope()).Returns(scope);

        Mock<IArchLucidUnitOfWork> uow = new();
        uow.SetupGet(x => x.SupportsExternalTransaction).Returns(false);
        uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);

        Mock<IArchLucidUnitOfWorkFactory> uowFactory = new();
        uowFactory.Setup(f => f.CreateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(uow.Object);

        Mock<IRunRepository> runRepo = new();
        runRepo.Setup(x => x.SaveAsync(It.IsAny<RunRecord>(), It.IsAny<CancellationToken>(), null, null))
            .Returns(Task.CompletedTask);

        Mock<IAuthorityPipelineStagesExecutor> pipeline = new();
        Mock<IRetrievalIndexingOutboxRepository> retrievalOutbox = new();
        Mock<IAuthorityPipelineWorkRepository> workRepo = new();
        workRepo.Setup(x => x.EnqueueAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IAsyncAuthorityPipelineModeResolver> modeResolver = new();
        modeResolver
            .Setup(x => x.ShouldQueueContextAndGraphStagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Mock<IAuditService> audit = new();
        audit.Setup(x => x.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IIntegrationEventPublisher> integrationEvents = new();
        integrationEvents
            .Setup(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        integrationEvents
            .Setup(x => x.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IIntegrationEventOutboxRepository> integrationOutbox = new();
        StubIntegrationOutbox(integrationOutbox);
        Mock<IOptionsMonitor<IntegrationEventsOptions>> integrationEventOpts = CreateIntegrationEventsOptionsMonitor(false);

        AuthorityRunOrchestrator sut = new(
            uowFactory.Object,
            scopeProvider.Object,
            audit.Object,
            runRepo.Object,
            pipeline.Object,
            retrievalOutbox.Object,
            workRepo.Object,
            modeResolver.Object,
            integrationEvents.Object,
            integrationOutbox.Object,
            integrationEventOpts.Object,
            NullLogger<AuthorityRunOrchestrator>.Instance);

        ContextIngestionRequest request = new() { ProjectId = "q", Description = "d" };

        RunRecord result = await sut.ExecuteAsync(request, CancellationToken.None, evidenceBundleIdForDeferredWork: "evidence-bundle");

        result.ContextSnapshotId.Should().BeNull();
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        pipeline.Verify(
            x => x.ExecuteAfterRunPersistedAsync(It.IsAny<AuthorityPipelineContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
        retrievalOutbox.Verify(
            x => x.EnqueueAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        workRepo.Verify(
            x => x.EnqueueAsync(
                result.RunId,
                scope.TenantId,
                scope.WorkspaceId,
                scope.ProjectId,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        audit.Verify(
            x => x.LogAsync(
                It.Is<AuditEvent>(e =>
                    e.EventType == AuditEventTypes.RunStarted
                    && e.RunId == result.RunId
                    && JsonDocument.Parse(e.DataJson).RootElement.GetProperty("queued").GetBoolean()),
                It.IsAny<CancellationToken>()),
            Times.Once);
        audit.Verify(
            x => x.LogAsync(
                It.Is<AuditEvent>(e => e.EventType == AuditEventTypes.RunCompleted),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CompleteQueuedAuthorityPipelineAsync_emits_run_started_with_resumed_from_queue()
    {
        Guid runIdGuid = Guid.NewGuid();
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
        };

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(x => x.GetCurrentScope()).Returns(scope);

        Mock<IArchLucidUnitOfWork> uow = new();
        uow.SetupGet(x => x.SupportsExternalTransaction).Returns(false);
        uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);

        Mock<IArchLucidUnitOfWorkFactory> uowFactory = new();
        uowFactory.Setup(f => f.CreateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(uow.Object);

        RunRecord existingRun = new()
        {
            RunId = runIdGuid,
            ProjectId = "resume-proj",
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ScopeProjectId = scope.ProjectId,
            ContextSnapshotId = null,
            CreatedUtc = DateTime.UtcNow,
        };

        Mock<IRunRepository> runRepo = new();
        runRepo.Setup(x => x.GetByIdAsync(scope, runIdGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRun);
        runRepo.Setup(x => x.SaveAsync(It.IsAny<RunRecord>(), It.IsAny<CancellationToken>(), null, null))
            .Returns(Task.CompletedTask);
        runRepo.Setup(x => x.UpdateAsync(It.IsAny<RunRecord>(), It.IsAny<CancellationToken>(), null, null))
            .Returns(Task.CompletedTask);

        Guid contextSnapshotId = Guid.NewGuid();
        Guid findingsId = Guid.NewGuid();
        Guid traceId = Guid.NewGuid();
        Guid manifestId = Guid.NewGuid();

        Mock<IAuthorityPipelineStagesExecutor> pipeline = new();
        pipeline
            .Setup(x => x.ExecuteAfterRunPersistedAsync(It.IsAny<AuthorityPipelineContext>(), It.IsAny<CancellationToken>()))
            .Callback<AuthorityPipelineContext, CancellationToken>(
                (ctx, _) =>
                {
                    ctx.ContextSnapshot = new ContextSnapshot
                    {
                        SnapshotId = contextSnapshotId,
                        RunId = ctx.Run.RunId,
                        ProjectId = ctx.Request.ProjectId,
                        CreatedUtc = DateTime.UtcNow,
                    };

                    ctx.FindingsSnapshot = new FindingsSnapshot
                    {
                        FindingsSnapshotId = findingsId,
                        RunId = ctx.Run.RunId,
                        ContextSnapshotId = contextSnapshotId,
                        GraphSnapshotId = Guid.NewGuid(),
                        CreatedUtc = DateTime.UtcNow,
                    };

                    ctx.Trace = RuleAuditTrace.From(new RuleAuditTracePayload
                    {
                        TenantId = ctx.Scope.TenantId,
                        WorkspaceId = ctx.Scope.WorkspaceId,
                        ProjectId = ctx.Scope.ProjectId,
                        DecisionTraceId = traceId,
                        RunId = ctx.Run.RunId,
                        CreatedUtc = DateTime.UtcNow,
                        RuleSetId = "rs",
                        RuleSetVersion = "1",
                        RuleSetHash = "h",
                    });

                    ctx.Manifest = NewMinimalManifest(
                        ctx.Scope,
                        ctx.Run.RunId,
                        contextSnapshotId,
                        Guid.NewGuid(),
                        findingsId,
                        traceId,
                        manifestId);
                })
            .Returns(Task.CompletedTask);

        Mock<IRetrievalIndexingOutboxRepository> retrievalOutbox = new();
        retrievalOutbox.Setup(x => x.EnqueueAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IAuthorityPipelineWorkRepository> workRepo = new();
        Mock<IAsyncAuthorityPipelineModeResolver> modeResolver = new();

        Mock<IAuditService> audit = new();
        audit.Setup(x => x.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IIntegrationEventPublisher> integrationEvents = new();
        integrationEvents
            .Setup(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        integrationEvents
            .Setup(x => x.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IIntegrationEventOutboxRepository> integrationOutbox = new();
        StubIntegrationOutbox(integrationOutbox);
        Mock<IOptionsMonitor<IntegrationEventsOptions>> integrationEventOpts = CreateIntegrationEventsOptionsMonitor(false);

        AuthorityRunOrchestrator sut = new(
            uowFactory.Object,
            scopeProvider.Object,
            audit.Object,
            runRepo.Object,
            pipeline.Object,
            retrievalOutbox.Object,
            workRepo.Object,
            modeResolver.Object,
            integrationEvents.Object,
            integrationOutbox.Object,
            integrationEventOpts.Object,
            NullLogger<AuthorityRunOrchestrator>.Instance);

        ContextIngestionRequest request = new()
        {
            RunId = runIdGuid,
            ProjectId = "resume-proj",
            Description = "d",
        };

        RunRecord result = await sut.CompleteQueuedAuthorityPipelineAsync(request, CancellationToken.None);

        result.RunId.Should().Be(runIdGuid);
        audit.Verify(
            x => x.LogAsync(
                It.Is<AuditEvent>(e =>
                    e.EventType == AuditEventTypes.RunStarted
                    && e.RunId == runIdGuid
                    && JsonDocument.Parse(e.DataJson).RootElement.GetProperty("queued").GetBoolean()
                    && JsonDocument.Parse(e.DataJson).RootElement.GetProperty("resumedFromQueue").GetBoolean()),
                It.IsAny<CancellationToken>()),
            Times.Once);
        audit.Verify(
            x => x.LogAsync(
                It.Is<AuditEvent>(e => e.EventType == AuditEventTypes.RunCompleted),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_when_pipeline_throws_rolls_back_without_commit()
    {
        ScopeContext scope = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
        };

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(x => x.GetCurrentScope()).Returns(scope);

        Mock<IArchLucidUnitOfWork> uow = new();
        uow.SetupGet(x => x.SupportsExternalTransaction).Returns(false);
        uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);

        Mock<IArchLucidUnitOfWorkFactory> uowFactory = new();
        uowFactory.Setup(f => f.CreateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(uow.Object);

        Mock<IRunRepository> runRepo = new();
        runRepo.Setup(x => x.SaveAsync(It.IsAny<RunRecord>(), It.IsAny<CancellationToken>(), null, null))
            .Returns(Task.CompletedTask);

        Mock<IAuthorityPipelineStagesExecutor> pipeline = new();
        pipeline
            .Setup(x => x.ExecuteAfterRunPersistedAsync(It.IsAny<AuthorityPipelineContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("pipeline failed"));

        Mock<IRetrievalIndexingOutboxRepository> retrievalOutbox = new();
        Mock<IAuthorityPipelineWorkRepository> workRepo = new();
        Mock<IAsyncAuthorityPipelineModeResolver> modeResolver = new();
        modeResolver
            .Setup(x => x.ShouldQueueContextAndGraphStagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Mock<IAuditService> audit = new();
        audit.Setup(x => x.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IIntegrationEventPublisher> integrationEvents = new();
        integrationEvents
            .Setup(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        integrationEvents
            .Setup(x => x.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IIntegrationEventOutboxRepository> integrationOutbox = new();
        StubIntegrationOutbox(integrationOutbox);
        Mock<IOptionsMonitor<IntegrationEventsOptions>> integrationEventOpts = CreateIntegrationEventsOptionsMonitor(false);

        AuthorityRunOrchestrator sut = new(
            uowFactory.Object,
            scopeProvider.Object,
            audit.Object,
            runRepo.Object,
            pipeline.Object,
            retrievalOutbox.Object,
            workRepo.Object,
            modeResolver.Object,
            integrationEvents.Object,
            integrationOutbox.Object,
            integrationEventOpts.Object,
            NullLogger<AuthorityRunOrchestrator>.Instance);

        ContextIngestionRequest request = new() { ProjectId = "proj-fail" };

        Func<Task> act = async () => await sut.ExecuteAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("pipeline failed");

        uow.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static void StubIntegrationOutbox(Mock<IIntegrationEventOutboxRepository> mock)
    {
        mock.Setup(x => x.EnqueueAsync(
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mock.Setup(x => x.EnqueueAsync(
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<IDbConnection>(),
                It.IsAny<IDbTransaction>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private static Mock<IOptionsMonitor<IntegrationEventsOptions>> CreateIntegrationEventsOptionsMonitor(
        bool transactionalOutboxEnabled)
    {
        Mock<IOptionsMonitor<IntegrationEventsOptions>> mock = new();
        mock.Setup(m => m.CurrentValue)
            .Returns(new IntegrationEventsOptions { TransactionalOutboxEnabled = transactionalOutboxEnabled });

        return mock;
    }

    private static GoldenManifest NewMinimalManifest(
        ScopeContext scope,
        Guid runId,
        Guid contextId,
        Guid graphId,
        Guid findingsId,
        Guid traceId,
        Guid manifestId)
    {
        return new GoldenManifest
        {
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId,
            ManifestId = manifestId,
            RunId = runId,
            ContextSnapshotId = contextId,
            GraphSnapshotId = graphId,
            FindingsSnapshotId = findingsId,
            DecisionTraceId = traceId,
            CreatedUtc = DateTime.UtcNow,
            ManifestHash = "pending",
            RuleSetId = "rs",
            RuleSetVersion = "1",
            RuleSetHash = "rsh",
            Metadata = new DecisioningManifestMetadata { Name = "orch-test" },
            Requirements = new RequirementsCoverageSection(),
            Topology = new TopologySection(),
            Security = new SecuritySection(),
            Compliance = new ComplianceSection(),
            Cost = new CostSection(),
            Constraints = new ConstraintSection(),
            UnresolvedIssues = new UnresolvedIssuesSection(),
            Assumptions = [],
            Warnings = [],
            Provenance = new ManifestProvenance(),
            Decisions = [],
        };
    }
}
