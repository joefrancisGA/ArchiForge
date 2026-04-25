using ArchLucid.Core.Scim.Filtering;
using ArchLucid.Core.Scim.Models;

namespace ArchLucid.Core.Scim;

public interface IScimUserRepository
{
    Task<(IReadOnlyList<ScimUserRecord> items, int totalCount)> ListAsync(
        Guid tenantId,
        ScimFilterNode? filter,
        int startIndex1Based,
        int count,
        CancellationToken cancellationToken);

    Task<ScimUserRecord?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<ScimUserRecord?> GetByExternalIdAsync(Guid tenantId, string externalId, CancellationToken cancellationToken);

    Task<ScimUserRecord> InsertAsync(
        Guid tenantId,
        string externalId,
        string userName,
        string? displayName,
        bool active,
        string? resolvedRole,
        CancellationToken cancellationToken);

    Task ReplaceAsync(
        Guid tenantId,
        Guid id,
        string externalId,
        string userName,
        string? displayName,
        bool active,
        string? resolvedRole,
        CancellationToken cancellationToken);

    Task PatchAsync(
        Guid tenantId,
        Guid id,
        string? externalId,
        string? userName,
        string? displayName,
        bool? active,
        string? resolvedRole,
        CancellationToken cancellationToken);

    Task DeactivateAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<(string DisplayName, string ExternalId)>> ListGroupKeysForUserAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken);
}
