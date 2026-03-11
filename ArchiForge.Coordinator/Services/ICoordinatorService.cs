using ArchiForge.Contracts.Requests;

namespace ArchiForge.Coordinator.Services;

public interface ICoordinatorService
{
    CoordinationResult CreateRun(ArchitectureRequest request);
}