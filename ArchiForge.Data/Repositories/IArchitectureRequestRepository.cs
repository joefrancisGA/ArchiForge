using ArchiForge.Contracts.Requests;

namespace ArchiForge.Data.Repositories;

public interface IArchitectureRequestRepository
{
    Task CreateAsync(ArchitectureRequest request, CancellationToken cancellationToken = default);
    Task<ArchitectureRequest?> GetByIdAsync(string requestId, CancellationToken cancellationToken = default);
}