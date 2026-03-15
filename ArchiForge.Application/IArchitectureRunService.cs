using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Contracts.Requests;

namespace ArchiForge.Application;

public interface IArchitectureRunService
{
    Task<CreateRunResult> CreateRunAsync(
        ArchitectureRequest request,
        CancellationToken cancellationToken = default);

    Task<ExecuteRunResult> ExecuteRunAsync(
        string runId,
        CancellationToken cancellationToken = default);

    Task<CommitRunResult> CommitRunAsync(
        string runId,
        CancellationToken cancellationToken = default);
}
