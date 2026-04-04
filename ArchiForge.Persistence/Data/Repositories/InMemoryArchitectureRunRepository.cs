using System.Text.Json;

using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Contracts.Requests;

namespace ArchiForge.Persistence.Data.Repositories;

/// <summary>
/// Thread-safe in-memory <see cref="IArchitectureRunRepository"/> for tests.
/// When <see cref="IArchitectureRequestRepository"/> is supplied, <see cref="ListAsync"/> resolves <see cref="ArchitectureRunListItem.SystemName"/> from stored requests.
/// </summary>
public sealed class InMemoryArchitectureRunRepository(IArchitectureRequestRepository? requestLookup = null)
    : IArchitectureRunRepository
{
    private readonly IArchitectureRequestRepository? _requestLookup = requestLookup;
    private readonly Dictionary<string, ArchitectureRun> _byRunId = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public Task CreateAsync(ArchitectureRun run, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        
            _byRunId[run.RunId] = Clone(run);
        

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<ArchitectureRun?> GetByIdAsync(string runId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        
            return Task.FromResult(_byRunId.TryGetValue(runId, out ArchitectureRun? r) ? Clone(r) : null);
        
    }

    /// <inheritdoc />
    public Task UpdateStatusAsync(
        string runId,
        ArchitectureRunStatus status,
        string? currentManifestVersion = null,
        DateTime? completedUtc = null,
        CancellationToken cancellationToken = default,
        ArchitectureRunStatus? expectedStatus = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_byRunId.TryGetValue(runId, out ArchitectureRun? run))
            {
                if (expectedStatus.HasValue)
                
                    throw new InvalidOperationException(
                        $"Run '{runId}' could not be transitioned to '{status}': " +
                        $"expected status '{expectedStatus}' but the run has already been moved by a concurrent operation.");
                

                return Task.CompletedTask;
            }

            if (expectedStatus.HasValue && run.Status != expectedStatus.Value)
            
                throw new InvalidOperationException(
                    $"Run '{runId}' could not be transitioned to '{status}': " +
                    $"expected status '{expectedStatus}' but the run has already been moved by a concurrent operation.");
            

            run.Status = status;
            if (currentManifestVersion is not null)
            
                run.CurrentManifestVersion = currentManifestVersion;
            

            run.CompletedUtc = completedUtc;
            _byRunId[runId] = Clone(run);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ApplyDeferredAuthoritySnapshotsAsync(
        string runId,
        string? contextSnapshotId,
        Guid? graphSnapshotId,
        Guid? artifactBundleId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_byRunId.TryGetValue(runId, out ArchitectureRun? run))
            {
                throw new InvalidOperationException($"Run '{runId}' was not found for deferred authority promotion.");
            }

            if (run.Status != ArchitectureRunStatus.Created)
            {
                throw new InvalidOperationException(
                    $"Run '{runId}' could not apply deferred authority snapshots: expected status '{ArchitectureRunStatus.Created}'.");
            }

            run.ContextSnapshotId = contextSnapshotId;
            run.GraphSnapshotId = graphSnapshotId;
            run.ArtifactBundleId = artifactBundleId;
            run.Status = ArchitectureRunStatus.TasksGenerated;
            _byRunId[runId] = Clone(run);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ArchitectureRunListItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<ArchitectureRun> snapshot;
        lock (_gate)
        
            snapshot = _byRunId.Values.Select(Clone).ToList();
        

        List<ArchitectureRunListItem> items = [];
        foreach (ArchitectureRun run in snapshot.OrderByDescending(r => r.CreatedUtc).ThenByDescending(r => r.RunId, StringComparer.Ordinal).Take(200))
        {
            string systemName = string.Empty;
            if (_requestLookup is not null)
            {
                ArchitectureRequest? req = await _requestLookup
                    .GetByIdAsync(run.RequestId, cancellationToken)
                    ;

                systemName = req?.SystemName ?? string.Empty;
            }

            items.Add(new ArchitectureRunListItem
            {
                RunId = run.RunId,
                RequestId = run.RequestId,
                Status = run.Status.ToString(),
                CreatedUtc = run.CreatedUtc,
                CompletedUtc = run.CompletedUtc,
                CurrentManifestVersion = run.CurrentManifestVersion,
                SystemName = systemName,
            });
        }

        return items;
    }

    private static ArchitectureRun Clone(ArchitectureRun source)
    {
        string json = JsonSerializer.Serialize(source, ContractJson.Default);
        ArchitectureRun? copy = JsonSerializer.Deserialize<ArchitectureRun>(json, ContractJson.Default);

        return copy ?? throw new InvalidOperationException("Clone produced null ArchitectureRun.");
    }
}
