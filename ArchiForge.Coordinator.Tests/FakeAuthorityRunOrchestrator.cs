using ArchiForge.ContextIngestion.Models;
using ArchiForge.Persistence.Models;
using ArchiForge.Persistence.Orchestration;

namespace ArchiForge.Coordinator.Tests;

/// <summary>Test double: skips real authority pipeline; returns a run with synthetic snapshot IDs.</summary>
internal sealed class FakeAuthorityRunOrchestrator : IAuthorityRunOrchestrator
{
    public Task<RunRecord> ExecuteAsync(ContextIngestionRequest request, CancellationToken ct)
    {
        _ = ct;
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
}
