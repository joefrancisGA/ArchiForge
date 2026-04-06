using ArchiForge.Contracts.Requests;

namespace ArchiForge.Persistence.Data.Repositories;

/// <summary>
/// Persistence contract for <see cref="ArchitectureRequest"/> records.
/// </summary>
public interface IArchitectureRequestRepository
{
    /// <summary>
    /// Persists a new architecture request.
    /// <paramref name="request"/> must have a non-empty <c>RequestId</c>.
    /// </summary>
    Task CreateAsync(ArchitectureRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the architecture request with the specified <paramref name="requestId"/>,
    /// or <see langword="null"/> when not found.
    /// </summary>
    Task<ArchitectureRequest?> GetByIdAsync(string requestId, CancellationToken cancellationToken = default);
}
