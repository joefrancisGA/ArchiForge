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

        if (patch.ValueKind != JsonValueKind.Object || !patch.TryGetProperty("Operations", out JsonElement ops))
            throw new ScimUserResourceParseException("invalidSyntax", "PATCH must include Operations.");

        List<Guid> members = [];

        foreach (JsonElement op in ops.EnumerateArray().Where(op => op.ValueKind == JsonValueKind.Object))
        {
            string? opName = op.TryGetProperty("op", out JsonElement on) && on.ValueKind == JsonValueKind.String
                ? on.GetString()
                : null;

            if (!string.Equals(opName, "add", StringComparison.OrdinalIgnoreCase))
                throw new ScimUserResourceParseException("invalidPath", "Only 'add' on members is supported in v1.");

            if (!op.TryGetProperty("path", out JsonElement pathEl) || pathEl.GetString() is not { } p ||
                !string.Equals(p, "members", StringComparison.OrdinalIgnoreCase))
                throw new ScimUserResourceParseException("invalidPath", "Only path 'members' is supported.");

            if (!op.TryGetProperty("value", out JsonElement val))
                throw new ScimUserResourceParseException("invalidValue", "members add requires value.");

            if (val.ValueKind != JsonValueKind.Array)
                continue;

            foreach (JsonElement m in val.EnumerateArray())
            {
                if (m.TryGetProperty("value", out JsonElement idEl) && idEl.ValueKind == JsonValueKind.String &&
                    Guid.TryParse(idEl.GetString(), out Guid uid))
                    members.Add(uid);
            }
        }

        await _groups.SetMembersAsync(tenantId, id, members, cancellationToken);

        await LogAsync(
            tenantId,
            AuditEventTypes.ScimGroupMembershipChanged,
            $"{{\"groupId\":\"{id:D}\",\"memberCount\":{members.Count}}}",
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
