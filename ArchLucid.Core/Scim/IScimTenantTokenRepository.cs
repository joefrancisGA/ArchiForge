using ArchLucid.Core.Scim.Models;

namespace ArchLucid.Core.Scim;

public interface IScimTenantTokenRepository
{
    Task<ScimTokenRow?> FindActiveByPublicLookupKeyAsync(string publicLookupKey, CancellationToken cancellationToken);

    Task<Guid> InsertAsync(Guid tenantId, string publicLookupKey, byte[] secretHash, CancellationToken cancellationToken);

    Task<IReadOnlyList<ScimTokenSummaryRow>> ListForTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<bool> TryRevokeByIdAsync(Guid tenantId, Guid tokenId, CancellationToken cancellationToken);
}
