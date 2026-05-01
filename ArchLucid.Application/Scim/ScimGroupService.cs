using System.Text.Json;

using ArchLucid.Core.Audit;
using ArchLucid.Core.Scim;
using ArchLucid.Core.Scim.Models;
using ArchLucid.Core.Scoping;

namespace ArchLucid.Application.Scim;

public sealed class ScimGroupService(IScimGroupRepository groups, IAuditService audit) : IScimGroupService
{
    private readonly IScimGroupRepository _groups = groups ?? throw new ArgumentNullException(nameof(groups));

    private readonly IAuditService _audit = audit ?? throw new ArgumentNullException(nameof(audit));

    /// <inheritdoc />
    public Task<(IReadOnlyList<ScimGroupRecord> items, int totalResults)> ListAsync(
        Guid tenantId,
        int startIndex,
        int count,
        CancellationToken cancellationToken)
    {
        return _groups.ListAsync(tenantId, startIndex, Math.Clamp(count, 0, 200), cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScimGroupRecord?> GetAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        return _groups.GetByIdAsync(tenantId, id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ScimGroupRecord> CreateAsync(Guid tenantId, JsonElement resource, CancellationToken cancellationToken)
    {
        (string displayName, string externalId) = ScimGroupResourceParser.ParseGroup(resource);
        ScimGroupRecord g = await _groups.InsertAsync(tenantId, externalId, displayName, cancellationToken);

        await LogAsync(
            tenantId,
            AuditEventTypes.ScimGroupProvisioned,
            $"{{\"groupId\":\"{g.Id:D}\",\"externalId\":\"{JsonSerializer.Serialize(externalId).Trim('"')}\"}}",
            cancellationToken);

        return g;
    }

    /// <inheritdoc />
    public async Task ReplaceAsync(Guid tenantId, Guid id, JsonElement resource, CancellationToken cancellationToken)
    {
        _ = await _groups.GetByIdAsync(tenantId, id, cancellationToken)
            ?? throw new ScimNotFoundException("Group not found.");

        (string displayName, string externalId) = ScimGroupResourceParser.ParseGroup(resource);

        await _groups.ReplaceAsync(tenantId, id, externalId, displayName, cancellationToken);

        await LogAsync(tenantId, AuditEventTypes.ScimGroupMembershipChanged, $"{{\"groupId\":\"{id:D}\"}}", cancellationToken);
    }

    /// <inheritdoc />
    public async Task PatchMembersAsync(Guid tenantId, Guid id, JsonElement patch, CancellationToken cancellationToken)
    {
        _ = await _groups.GetByIdAsync(tenantId, id, cancellationToken)
            ?? throw new ScimNotFoundException("Group not found.");

        if (patch.ValueKind != JsonValueKind.Object || !patch.TryGetProperty("Operations", out JsonElement ops) ||
            ops.ValueKind != JsonValueKind.Array)
            throw new ScimUserResourceParseException("invalidSyntax", "PATCH must include Operations.");

        IReadOnlyList<Guid> initial = await _groups.ListMemberUserIdsAsync(tenantId, id, cancellationToken);

        HashSet<Guid> working = new(initial);

        ScimGroupMemberPatchPlanner.ApplyOrderedOperations(ops, working);

        List<Guid> ordered = working.OrderBy(static u => u).ToList();

        await _groups.SetMembersAsync(tenantId, id, ordered, cancellationToken);

        await LogAsync(
            tenantId,
            AuditEventTypes.ScimGroupMembershipChanged,
            $"{{\"groupId\":\"{id:D}\",\"memberCount\":{ordered.Count}}}",
            cancellationToken);
    }

    private async Task LogAsync(Guid tenantId, string eventType, string dataJson, CancellationToken ct)
    {
        await _audit.LogAsync(
            new AuditEvent
            {
                EventType = eventType,
                ActorUserId = "scim",
                ActorUserName = "SCIM provisioning",
                TenantId = tenantId,
                WorkspaceId = ScopeIds.DefaultWorkspace,
                ProjectId = ScopeIds.DefaultProject,
                DataJson = dataJson
            },
            ct);
    }
}
