using System.Text.Json;

using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Governance;

namespace ArchiForge.Persistence.Data.Repositories;

/// <summary>
/// Thread-safe in-memory <see cref="IGovernancePromotionRecordRepository"/> (JSON clone-on-read).
/// </summary>
public sealed class InMemoryGovernancePromotionRecordRepository : IGovernancePromotionRecordRepository
{
    private const int MaxListRows = 200;
    private readonly Dictionary<string, GovernancePromotionRecord> _byId = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public Task CreateAsync(GovernancePromotionRecord item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(item.PromotionRecordId))
        
            throw new ArgumentException("PromotionRecordId is required.", nameof(item));
        

        GovernancePromotionRecord stored = Clone(item);

        lock (_gate)
        
            _byId[stored.PromotionRecordId] = stored;
        

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<GovernancePromotionRecord>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            List<GovernancePromotionRecord> ordered = _byId.Values
                .Where(x => string.Equals(x.RunId, runId, StringComparison.Ordinal))
                .OrderByDescending(x => x.PromotedUtc)
                .Take(MaxListRows)
                .Select(Clone)
                .ToList();

            return Task.FromResult<IReadOnlyList<GovernancePromotionRecord>>(ordered);
        }
    }

    private static GovernancePromotionRecord Clone(GovernancePromotionRecord source)
    {
        string json = JsonSerializer.Serialize(source, ContractJson.Default);
        GovernancePromotionRecord? copy = JsonSerializer.Deserialize<GovernancePromotionRecord>(json, ContractJson.Default);

        return copy ?? throw new InvalidOperationException("Clone produced null GovernancePromotionRecord.");
    }
}
