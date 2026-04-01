using System.Text.Json;

using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;

namespace ArchiForge.Data.Repositories;

/// <summary>
/// Thread-safe in-memory <see cref="IEvidenceBundleRepository"/> for tests (JSON clone-on-read).
/// </summary>
public sealed class InMemoryEvidenceBundleRepository : IEvidenceBundleRepository
{
    private readonly Dictionary<string, EvidenceBundle> _byId = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public Task CreateAsync(EvidenceBundle evidenceBundle, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evidenceBundle);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            _byId[evidenceBundle.EvidenceBundleId] = Clone(evidenceBundle);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<EvidenceBundle?> GetByIdAsync(string evidenceBundleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(_byId.TryGetValue(evidenceBundleId, out EvidenceBundle? b) ? Clone(b) : null);
        }
    }

    private static EvidenceBundle Clone(EvidenceBundle source)
    {
        string json = JsonSerializer.Serialize(source, ContractJson.Default);
        EvidenceBundle? copy = JsonSerializer.Deserialize<EvidenceBundle>(json, ContractJson.Default);

        return copy ?? throw new InvalidOperationException("Clone produced null EvidenceBundle.");
    }
}
