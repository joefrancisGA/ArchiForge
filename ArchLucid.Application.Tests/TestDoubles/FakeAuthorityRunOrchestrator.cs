using ArchLucid.ContextIngestion.Models;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Orchestration;

namespace ArchLucid.Application.Tests.TestDoubles;

/// <summary>Test double: skips real authority pipeline; returns a run with synthetic snapshot IDs.</summary>
internal sealed class FakeAuthorityRunOrchestrator : IAuthorityRunOrchestrator
{
    public Task<RunRecord> ExecuteAsync(
        ContextIngestionRequest request,
        CancellationToken cancellationToken = default,
        string? evidenceBundleIdForDeferredWork = null)
    {
        _ = cancellationToken;
        _ = evidenceBundleIdForDeferredWork;
        Guid runId = Guid.NewGuid();
        return Task.FromResult(new RunRecord
        {
            RunId = runId,
            ProjectId = request.ProjectId,
            Description = request.Description,
            CreatedUtc = DateTime.UtcNow,
            ContextSnapshotId = Guid.NewGuid(),
            GraphSnapshotId = Guid.NewGuid(),
            FindingsSnapshotId = Guid.NewGuid(),
            GoldenManifestId = Guid.NewGuid(),
            DecisionTraceId = Guid.NewGuid(),
            ArtifactBundleId = Guid.NewGuid()
        });
    }

    /// <inheritdoc />
    public Task<RunRecord> CompleteQueuedAuthorityPipelineAsync(
        ContextIngestionRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        return Task.FromResult(new RunRecord
        {
            RunId = request.RunId,
            ProjectId = request.ProjectId,
            Description = request.Description,
            CreatedUtc = DateTime.UtcNow,
            ContextSnapshotId = Guid.NewGuid(),
            GraphSnapshotId = Guid.NewGuid(),
            FindingsSnapshotId = Guid.NewGuid(),
            GoldenManifestId = Guid.NewGuid(),
            DecisionTraceId = Guid.NewGuid(),
            ArtifactBundleId = Guid.NewGuid(),
        });
    }
}
