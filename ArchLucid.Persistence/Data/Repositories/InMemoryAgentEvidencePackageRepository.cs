using System.Data;
using System.Text.Json;

using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>
///     Thread-safe in-memory <see cref="IAgentEvidencePackageRepository" /> for tests (JSON clone-on-read).
/// </summary>
public sealed class InMemoryAgentEvidencePackageRepository : IAgentEvidencePackageRepository
{
    private readonly Dictionary<string, AgentEvidencePackage> _byId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AgentEvidencePackage> _byRunId = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public Task CreateAsync(
        AgentEvidencePackage evidencePackage,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(evidencePackage);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (_byRunId.TryGetValue(evidencePackage.RunId, out AgentEvidencePackage? prior))

                _byId.Remove(prior.EvidencePackageId);


            AgentEvidencePackage copy = Clone(evidencePackage);
            _byId[copy.EvidencePackageId] = copy;
            _byRunId[copy.RunId] = copy;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<AgentEvidencePackage?> GetByRunIdAsync(string runId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)

            return Task.FromResult(_byRunId.TryGetValue(runId, out AgentEvidencePackage? p) ? Clone(p) : null);
    }

    /// <inheritdoc />
    public Task<AgentEvidencePackage?> GetByIdAsync(string evidencePackageId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)

            return Task.FromResult(_byId.TryGetValue(evidencePackageId, out AgentEvidencePackage? p) ? Clone(p) : null);
    }

    private static AgentEvidencePackage Clone(AgentEvidencePackage source)
    {
        string json = JsonSerializer.Serialize(source, ContractJson.Default);
        AgentEvidencePackage? copy = JsonSerializer.Deserialize<AgentEvidencePackage>(json, ContractJson.Default);

        return copy ?? throw new InvalidOperationException("Clone produced null AgentEvidencePackage.");
    }
}
