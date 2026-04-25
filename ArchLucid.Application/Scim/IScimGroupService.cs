using System.Text.Json;

using ArchLucid.Core.Scim.Models;

namespace ArchLucid.Application.Scim;

public interface IScimGroupService
{
    Task<(IReadOnlyList<ScimGroupRecord> items, int totalResults)> ListAsync(
        Guid tenantId,
        int startIndex,
        int count,
        CancellationToken cancellationToken);

    Task<ScimGroupRecord?> GetAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<ScimGroupRecord> CreateAsync(Guid tenantId, JsonElement resource, CancellationToken cancellationToken);

    Task ReplaceAsync(Guid tenantId, Guid id, JsonElement resource, CancellationToken cancellationToken);

    Task PatchMembersAsync(Guid tenantId, Guid id, JsonElement patch, CancellationToken cancellationToken);
}
