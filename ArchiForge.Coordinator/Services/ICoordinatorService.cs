using ArchiForge.Contracts.Requests;

namespace ArchiForge.Coordinator.Services;

public interface ICoordinatorService
{
    Task<CoordinationResult> CreateRunAsync(
        ArchitectureRequest request,
        CancellationToken cancellationToken = default);
}