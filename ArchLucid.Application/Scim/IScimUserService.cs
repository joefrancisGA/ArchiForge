using System.Text.Json;

using ArchLucid.Core.Scim.Models;

namespace ArchLucid.Application.Scim;

public interface IScimUserService
{
    Task<(IReadOnlyList<ScimUserRecord> items, int totalResults)> ListAsync(
        Guid tenantId,
        string? filter,
        int startIndex,
        int count,
        CancellationToken cancellationToken);

    Task<ScimUserRecord?> GetAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<ScimUserRecord> CreateAsync(Guid tenantId, JsonElement resource, CancellationToken cancellationToken);

    Task ReplaceAsync(Guid tenantId, Guid id, JsonElement resource, CancellationToken cancellationToken);

    Task PatchAsync(Guid tenantId, Guid id, JsonElement patch, CancellationToken cancellationToken);

    Task DeactivateAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);
}
