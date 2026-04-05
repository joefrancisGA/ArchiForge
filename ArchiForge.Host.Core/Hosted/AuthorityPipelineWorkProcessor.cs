using ArchiForge.ContextIngestion.Models;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Contracts.Requests;
using ArchiForge.Coordinator.Services;
using ArchiForge.Core.Scoping;
using ArchiForge.Persistence.Data.Repositories;
using ArchiForge.Persistence.Models;
using ArchiForge.Persistence.Orchestration;

namespace ArchiForge.Host.Core.Hosted;

/// <inheritdoc cref="IAuthorityPipelineWorkProcessor" />
public sealed class AuthorityPipelineWorkProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<AuthorityPipelineWorkProcessor> logger) : IAuthorityPipelineWorkProcessor
{
    private readonly IServiceScopeFactory _scopeFactory =
        scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

    private readonly ILogger<AuthorityPipelineWorkProcessor> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task ProcessPendingBatchAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IAuthorityPipelineWorkRepository workOutbox =
            scope.ServiceProvider.GetRequiredService<IAuthorityPipelineWorkRepository>();

        IReadOnlyList<AuthorityPipelineWorkOutboxEntry> batch =
            await workOutbox.DequeuePendingAsync(25, cancellationToken);

        foreach (AuthorityPipelineWorkOutboxEntry entry in batch)
        {
            try
            {
                await ProcessEntryAsync(scope, entry, workOutbox, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(
                    ex,
                    "Authority pipeline work failed for outbox {OutboxId}, run {RunId}.",
                    entry.OutboxId,
                    entry.RunId);
            }
        }
    }

    private async Task ProcessEntryAsync(
        IServiceScope scope,
        AuthorityPipelineWorkOutboxEntry entry,
        IAuthorityPipelineWorkRepository workOutbox,
        CancellationToken cancellationToken)
    {
        AuthorityPipelineWorkPayload? payload = AuthorityPipelineWorkPayloadJson.Deserialize(entry.PayloadJson);

        if (payload?.ContextIngestionRequest is null ||
            string.IsNullOrWhiteSpace(payload.EvidenceBundleId))
        {
            _logger.LogError(
                "Authority pipeline work outbox {OutboxId} has invalid payload; marking processed.",
                entry.OutboxId);
            await workOutbox.MarkProcessedAsync(entry.OutboxId, cancellationToken);

            return;
        }

        ScopeContext jobScope = new()
        {
            TenantId = entry.TenantId,
            WorkspaceId = entry.WorkspaceId,
            ProjectId = entry.ProjectId,
        };

        using (IDisposable _ = AmbientScopeContext.Push(jobScope))
        {
            IAuthorityRunOrchestrator orchestrator =
                scope.ServiceProvider.GetRequiredService<IAuthorityRunOrchestrator>();
            IArchitectureRunRepository architectureRunRepository =
                scope.ServiceProvider.GetRequiredService<IArchitectureRunRepository>();
            IArchitectureRequestRepository requestRepository =
                scope.ServiceProvider.GetRequiredService<IArchitectureRequestRepository>();
            IEvidenceBundleRepository evidenceBundleRepository =
                scope.ServiceProvider.GetRequiredService<IEvidenceBundleRepository>();
            IAgentTaskRepository taskRepository =
                scope.ServiceProvider.GetRequiredService<IAgentTaskRepository>();

            ContextIngestionRequest request = payload.ContextIngestionRequest;
            request.RunId = entry.RunId;

            RunRecord completed =
                await orchestrator.CompleteQueuedAuthorityPipelineAsync(request, cancellationToken);

            string runIdN = entry.RunId.ToString("N");
            ArchitectureRun? architectureRun =
                await architectureRunRepository.GetByIdAsync(runIdN, cancellationToken);

            if (architectureRun is null)
            {
                _logger.LogError(
                    "Architecture run {RunId} missing after authority completion; marking authority work processed.",
                    runIdN);
                await workOutbox.MarkProcessedAsync(entry.OutboxId, cancellationToken);

                return;
            }

            ArchitectureRequest? architectureRequest =
                await requestRepository.GetByIdAsync(architectureRun.RequestId, cancellationToken);

            EvidenceBundle? evidenceBundle =
                await evidenceBundleRepository.GetByIdAsync(payload.EvidenceBundleId.Trim(), cancellationToken);

            if (architectureRequest is null || evidenceBundle is null)
            {
                _logger.LogError(
                    "Cannot promote deferred run {RunId}: request or evidence bundle missing.",
                    runIdN);
                await workOutbox.MarkProcessedAsync(entry.OutboxId, cancellationToken);

                return;
            }

            List<AgentTask> starterTasks =
                RunStarterTaskFactory.BuildStarterTasks(runIdN, evidenceBundle, architectureRequest);

            if (architectureRun.Status == ArchitectureRunStatus.Created)
            {
                await architectureRunRepository.ApplyDeferredAuthoritySnapshotsAsync(
                    runIdN,
                    completed.ContextSnapshotId?.ToString("N"),
                    completed.GraphSnapshotId,
                    completed.ArtifactBundleId,
                    cancellationToken);
            }

            ArchitectureRun? refreshed =
                await architectureRunRepository.GetByIdAsync(runIdN, cancellationToken);

            if (refreshed is not null && refreshed.TaskIds.Count == 0)
            {
                await taskRepository.CreateManyAsync(starterTasks, cancellationToken);
            }

            await workOutbox.MarkProcessedAsync(entry.OutboxId, cancellationToken);
        }
    }
}
