using ArchLucid.Core.Scim.Models;

namespace ArchLucid.Core.Scim;

public interface IScimGroupRepository
{
    Task<(IReadOnlyList<ScimGroupRecord> items, int totalCount)> ListAsync(
        Guid tenantId,
        int startIndex1Based,
        int count,
        CancellationToken cancellationToken);

    Task<ScimGroupRecord?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<ScimGroupRecord> InsertAsync(
        Guid tenantId,
        string externalId,
        string displayName,
        CancellationToken cancellationToken);

    Task ReplaceAsync(
        Guid tenantId,
        Guid id,
        string externalId,
        string displayName,
        CancellationToken cancellationToken);

    Task SetMembersAsync(Guid tenantId, Guid groupId, IReadOnlyList<Guid> userIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> ListMemberUserIdsAsync(Guid tenantId, Guid groupId, CancellationToken cancellationToken);
}
