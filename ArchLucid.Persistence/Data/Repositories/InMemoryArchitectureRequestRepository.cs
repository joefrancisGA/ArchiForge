using System.Text.Json;

using ArchLucid.Contracts.Common;
using System.Data;

using ArchLucid.Contracts.Requests;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>
/// Thread-safe in-memory <see cref="IArchitectureRequestRepository"/> for tests (JSON clone-on-read).
/// </summary>
public sealed class InMemoryArchitectureRequestRepository : IArchitectureRequestRepository
{
    private readonly Dictionary<string, ArchitectureRequest> _byId = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public Task CreateAsync(
        ArchitectureRequest request,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(request.RequestId))
            throw new ArgumentException("RequestId is required.", nameof(request));

        lock (_gate)
        
            _byId[request.RequestId] = Clone(request);
        

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<ArchitectureRequest?> GetByIdAsync(string requestId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        
            return Task.FromResult(_byId.TryGetValue(requestId, out ArchitectureRequest? r) ? Clone(r) : null);
        
    }

    private static ArchitectureRequest Clone(ArchitectureRequest source)
    {
        string json = JsonSerializer.Serialize(source, ContractJson.Default);
        ArchitectureRequest? copy = JsonSerializer.Deserialize<ArchitectureRequest>(json, ContractJson.Default);

        return copy ?? throw new InvalidOperationException("Clone produced null ArchitectureRequest.");
    }
}
