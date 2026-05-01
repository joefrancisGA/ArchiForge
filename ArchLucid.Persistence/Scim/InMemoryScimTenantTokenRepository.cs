using System.Collections.Concurrent;

using ArchLucid.Core.Scim;
using ArchLucid.Core.Scim.Models;

namespace ArchLucid.Persistence.Scim;

public sealed class InMemoryScimTenantTokenRepository : IScimTenantTokenRepository
{
    private readonly ConcurrentDictionary<string, ScimTokenRow> _byPublicKey = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Task<IReadOnlyList<ScimTokenRotationCandidate>> ListActiveCreatedOnOrBeforeAsync(
        DateTimeOffset createdUtcUpperBoundInclusive,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        List<ScimTokenRotationCandidate> list = _byPublicKey.Values
            .Where(r => r.RevokedUtc is null && r.CreatedUtc <= createdUtcUpperBoundInclusive)
            .Select(static r => new ScimTokenRotationCandidate(r.Id, r.TenantId, r.CreatedUtc))
            .ToList();

        return Task.FromResult<IReadOnlyList<ScimTokenRotationCandidate>>(list);
    }

    /// <inheritdoc />
    public Task<ScimTokenRow?> FindActiveByPublicLookupKeyAsync(string publicLookupKey, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        return Task.FromResult(
            _byPublicKey.TryGetValue(publicLookupKey, out ScimTokenRow? row) && row.RevokedUtc is null ? row : null);
    }

    /// <inheritdoc />
    public Task<Guid> InsertAsync(Guid tenantId, string publicLookupKey, byte[] secretHash, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        Guid id = Guid.NewGuid();
        ScimTokenRow row = new()
        {
            Id = id,
            TenantId = tenantId,
            PublicLookupKey = publicLookupKey,
            SecretHash = secretHash,
            CreatedUtc = DateTimeOffset.UtcNow,
            RevokedUtc = null
        };

        if (!_byPublicKey.TryAdd(publicLookupKey, row))
            throw new InvalidOperationException("Duplicate SCIM token public key.");

        return Task.FromResult(id);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ScimTokenSummaryRow>> ListForTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        IReadOnlyList<ScimTokenSummaryRow> list = _byPublicKey.Values
            .Where(r => r.TenantId == tenantId)
            .OrderByDescending(static r => r.CreatedUtc)
            .Select(static r => new ScimTokenSummaryRow
            {
                Id = r.Id,
                CreatedUtc = r.CreatedUtc,
                RevokedUtc = r.RevokedUtc,
                PublicLookupKey = r.PublicLookupKey
            })
            .ToList();

        return Task.FromResult(list);
    }

    /// <inheritdoc />
    public Task<bool> TryRevokeByIdAsync(Guid tenantId, Guid tokenId, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        foreach (KeyValuePair<string, ScimTokenRow> pair in _byPublicKey.ToArray())
        {
            ScimTokenRow r = pair.Value;

            if (r.Id != tokenId || r.TenantId != tenantId || r.RevokedUtc is not null)
                continue;


            ScimTokenRow revoked = new()
            {
                Id = r.Id,
                TenantId = r.TenantId,
                PublicLookupKey = r.PublicLookupKey,
                SecretHash = r.SecretHash,
                CreatedUtc = r.CreatedUtc,
                RevokedUtc = DateTimeOffset.UtcNow
            };

            _byPublicKey[pair.Key] = revoked;

            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}
