using System.Text.Json;

using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Governance;

namespace ArchiForge.Persistence.Data.Repositories;

/// <summary>
/// Thread-safe in-memory <see cref="IGovernanceEnvironmentActivationRepository"/>.
/// <see cref="UpdateAsync"/> mutates only <see cref="GovernanceEnvironmentActivation.IsActive"/> on the stored row,
/// matching the Dapper repository's SQL update surface.
/// </summary>
public sealed class InMemoryGovernanceEnvironmentActivationRepository : IGovernanceEnvironmentActivationRepository
{
    private const int MaxListRows = 200;
    private readonly Dictionary<string, GovernanceEnvironmentActivation> _byActivationId = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public Task CreateAsync(GovernanceEnvironmentActivation item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(item.ActivationId))
        
            throw new ArgumentException("ActivationId is required.", nameof(item));
        

        GovernanceEnvironmentActivation stored = Clone(item);

        lock (_gate)
        
            _byActivationId[stored.ActivationId] = stored;
        

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(GovernanceEnvironmentActivation item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        
            if (_byActivationId.TryGetValue(item.ActivationId, out GovernanceEnvironmentActivation? existing))
            
                existing.IsActive = item.IsActive;
            
        

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<GovernanceEnvironmentActivation>> GetByEnvironmentAsync(
        string environment,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            List<GovernanceEnvironmentActivation> ordered = _byActivationId.Values
                .Where(x => string.Equals(x.Environment, environment, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.ActivatedUtc)
                .Take(MaxListRows)
                .Select(Clone)
                .ToList();

            return Task.FromResult<IReadOnlyList<GovernanceEnvironmentActivation>>(ordered);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<GovernanceEnvironmentActivation>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            List<GovernanceEnvironmentActivation> ordered = _byActivationId.Values
                .Where(x => string.Equals(x.RunId, runId, StringComparison.Ordinal))
                .OrderByDescending(x => x.ActivatedUtc)
                .Take(MaxListRows)
                .Select(Clone)
                .ToList();

            return Task.FromResult<IReadOnlyList<GovernanceEnvironmentActivation>>(ordered);
        }
    }

    private static GovernanceEnvironmentActivation Clone(GovernanceEnvironmentActivation source)
    {
        string json = JsonSerializer.Serialize(source, ContractJson.Default);
        GovernanceEnvironmentActivation? copy =
            JsonSerializer.Deserialize<GovernanceEnvironmentActivation>(json, ContractJson.Default);

        return copy ?? throw new InvalidOperationException("Clone produced null GovernanceEnvironmentActivation.");
    }
}
