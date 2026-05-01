using System.Text.Json;

using ArchLucid.Application.Scim.Filtering;
using ArchLucid.Application.Scim.Patching;
using ArchLucid.Application.Scim.RoleMapping;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scim;
using ArchLucid.Core.Scim.Filtering;
using ArchLucid.Core.Scim.Models;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;

namespace ArchLucid.Application.Scim;

public sealed class ScimUserService(
    IScimUserRepository users,
    ITenantRepository tenants,
    IGroupToRoleMapper roleMapper,
    IAuditService audit) : IScimUserService
{
    internal const string ManualResolvedRoleFlatPath = "manualResolvedRole";

    private readonly IScimUserRepository _users = users ?? throw new ArgumentNullException(nameof(users));

    private readonly ITenantRepository _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));

    private readonly IGroupToRoleMapper _roleMapper = roleMapper ?? throw new ArgumentNullException(nameof(roleMapper));

    private readonly IAuditService _audit = audit ?? throw new ArgumentNullException(nameof(audit));

    /// <inheritdoc />
    public async Task<(IReadOnlyList<ScimUserRecord> items, int totalResults)> ListAsync(
        Guid tenantId,
        string? filter,
        int startIndex,
        int count,
        CancellationToken cancellationToken)
    {
        ScimFilterNode? ast = ScimFilterParser.Parse(filter);
        (IReadOnlyList<ScimUserRecord> items, int total) =
            await _users.ListAsync(tenantId, ast, startIndex, Math.Clamp(count, 0, 200), cancellationToken);

        return (items, total);
    }

    /// <inheritdoc />
    public Task<ScimUserRecord?> GetAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        return _users.GetByIdAsync(tenantId, id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ScimUserRecord> CreateAsync(Guid tenantId, JsonElement resource, CancellationToken cancellationToken)
    {
        (string userName, string? displayName, bool active, string externalId) = ScimUserResourceParser.ParseUser(resource);

        if (await _users.GetByExternalIdAsync(tenantId, externalId, cancellationToken) is not null)
            throw new ScimConflictException($"User with externalId '{externalId}' already exists.");

        if (active)
        {
            bool ok = await _tenants.TryIncrementEnterpriseScimSeatAsync(tenantId, cancellationToken);

            if (!ok)
                throw new ScimSeatLimitExceededException();
        }

        ScimUserRecord created =
            await _users.InsertAsync(tenantId, externalId, userName, displayName, active, null, ScimResolvedRoleOrigin.Unknown, cancellationToken);

        string? role = await ResolveRoleAsync(tenantId, created.Id, cancellationToken);
        ScimResolvedRoleOrigin origin =
            role is null ? ScimResolvedRoleOrigin.Unknown : ScimResolvedRoleOrigin.ScimGroups;

        if (!string.Equals(role, created.ResolvedRole, StringComparison.Ordinal))
            await _users.PatchAsync(tenantId, created.Id, null, null, null, null, role, origin, cancellationToken);


        created = await _users.GetByIdAsync(tenantId, created.Id, cancellationToken) ?? created;

        await LogAsync(
            tenantId,
            AuditEventTypes.ScimUserProvisioned,
            $"{{\"userId\":\"{created.Id:D}\",\"externalId\":\"{JsonEncoded(externalId)}\"}}",
            cancellationToken);

        return created;
    }

    /// <inheritdoc />
    public async Task ReplaceAsync(Guid tenantId, Guid id, JsonElement resource, CancellationToken cancellationToken)
    {
        ScimUserRecord existing = await _users.GetByIdAsync(tenantId, id, cancellationToken)
                                  ?? throw new ScimNotFoundException("User not found.");

        (string userName, string? displayName, bool active, string externalId) = ScimUserResourceParser.ParseUser(resource);

        await TransitionSeatAsync(tenantId, existing.Active, active, cancellationToken);

        string? manualFromBody = TryReadManualResolvedRoleFromUserResource(resource);
        string? groupRole = await ResolveRoleAsync(tenantId, id, cancellationToken);
        ResolveRoleChoices choices = DecideResolvedRole(existing, manualFromBody, groupRole);

        if (choices.ShouldEmitManualOverriddenAudit)
            await EmitRoleOverriddenAuditAsync(tenantId, existing, choices.FinalRole, cancellationToken);


        await _users.ReplaceAsync(
            tenantId,
            id,
            externalId,
            userName,
            displayName,
            active,
            choices.FinalRole,
            choices.FinalOrigin,
            cancellationToken);

        await LogAsync(
            tenantId,
            AuditEventTypes.ScimUserUpdated,
            $"{{\"userId\":\"{id:D}\",\"externalId\":\"{JsonEncoded(externalId)}\"}}",
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task PatchAsync(Guid tenantId, Guid id, JsonElement patch, CancellationToken cancellationToken)
    {
        ScimUserRecord existing = await _users.GetByIdAsync(tenantId, id, cancellationToken)
                                  ?? throw new ScimNotFoundException("User not found.");

        Dictionary<string, JsonElement> current = BuildFlatMap(existing);

        IReadOnlyDictionary<string, JsonElement> next;

        try
        {
            next = ScimPatchOpEvaluator.ApplyFlat(current, patch);
        }
        catch (ScimPatchException ex)
        {
            throw new ScimUserResourceParseException(ex.ScimType, ex.Message);
        }

        string? manualFromPatch =
            TryReadOptionalTrimmed(next, ManualResolvedRoleFlatPath, StringComparer.OrdinalIgnoreCase);

        Dictionary<string, JsonElement> core = ToCoreNextMap(next);

        bool nextActive = ReadActive(core, existing.Active);
        string externalId = ReadString(core, "externalId", existing.ExternalId);
        string userName = ReadString(core, "userName", existing.UserName);
        string? displayName = ReadOptionalString(core, "displayName", existing.DisplayName);

        await TransitionSeatAsync(tenantId, existing.Active, nextActive, cancellationToken);

        string? groupRole = await ResolveRoleAsync(tenantId, id, cancellationToken);
        ResolveRoleChoices choices = DecideResolvedRole(existing, manualFromPatch, groupRole);

        if (choices.ShouldEmitManualOverriddenAudit)
            await EmitRoleOverriddenAuditAsync(tenantId, existing, choices.FinalRole, cancellationToken);


        await _users.PatchAsync(
            tenantId,
            id,
            externalId,
            userName,
            displayName,
            nextActive,
            choices.FinalRole,
            choices.FinalOrigin,
            cancellationToken);

        await LogAsync(
            tenantId,
            AuditEventTypes.ScimUserUpdated,
            $"{{\"userId\":\"{id:D}\"}}",
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeactivateAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        ScimUserRecord existing = await _users.GetByIdAsync(tenantId, id, cancellationToken)
                                  ?? throw new ScimNotFoundException("User not found.");

        if (existing.Active)
            await _tenants.DecrementEnterpriseScimSeatAsync(tenantId, cancellationToken);


        await _users.DeactivateAsync(tenantId, id, cancellationToken);

        await LogAsync(
            tenantId,
            AuditEventTypes.ScimUserDeactivated,
            $"{{\"userId\":\"{id:D}\"}}",
            cancellationToken);
    }

    private static Dictionary<string, JsonElement> ToCoreNextMap(IReadOnlyDictionary<string, JsonElement> next)
    {
        Dictionary<string, JsonElement> core = new(next, StringComparer.OrdinalIgnoreCase);

        foreach (string k in core.Keys.Where(
                     static k => string.Equals(k, ManualResolvedRoleFlatPath, StringComparison.OrdinalIgnoreCase))
                     .ToList())
            core.Remove(k);

        return core;
    }

    private sealed record ResolveRoleChoices(
        string? FinalRole,
        ScimResolvedRoleOrigin FinalOrigin,
        bool ShouldEmitManualOverriddenAudit);

    private ResolveRoleChoices DecideResolvedRole(
        ScimUserRecord existing,
        string? manualFromRequest,
        string? groupMapped)
    {
        if (groupMapped is not null)
        {
            bool fire =
                existing.ResolvedRoleOrigin == ScimResolvedRoleOrigin.Manual
                && !string.Equals(existing.ResolvedRole, groupMapped, StringComparison.OrdinalIgnoreCase);

            return new ResolveRoleChoices(groupMapped, ScimResolvedRoleOrigin.ScimGroups, fire);
        }

        if (manualFromRequest is not null)
            return new ResolveRoleChoices(manualFromRequest, ScimResolvedRoleOrigin.Manual, false);


        return new ResolveRoleChoices(existing.ResolvedRole, existing.ResolvedRoleOrigin, false);
    }

    private Task EmitRoleOverriddenAuditAsync(Guid tenantId, ScimUserRecord existing, string? incomingGroupRole, CancellationToken ct)
    {
        string payload = JsonSerializer.Serialize(
            new
            {
                userId = existing.Id,
                fromRole = existing.ResolvedRole ?? string.Empty,
                toRole = incomingGroupRole ?? string.Empty
            });

        return LogAsync(tenantId, AuditEventTypes.RoleOverriddenByScim, payload, ct);
    }

    private async Task TransitionSeatAsync(Guid tenantId, bool wasActive, bool willBeActive, CancellationToken ct)
    {
        if (wasActive == willBeActive)
            return;

        if (willBeActive)
        {
            bool ok = await _tenants.TryIncrementEnterpriseScimSeatAsync(tenantId, ct);

            if (!ok)
                throw new ScimSeatLimitExceededException();

            return;
        }

        await _tenants.DecrementEnterpriseScimSeatAsync(tenantId, ct);
    }

    private static Dictionary<string, JsonElement> BuildFlatMap(ScimUserRecord u)
    {
        Dictionary<string, JsonElement> d = new(StringComparer.OrdinalIgnoreCase);
        using JsonDocument doc = JsonDocument.Parse(
            $$"""
              {"userName":{{JsonSerializer.Serialize(u.UserName)}},"displayName":{{JsonSerializer.Serialize(u.DisplayName ?? string.Empty)}},"active":{{(u.Active ? "true" : "false")}},"externalId":{{JsonSerializer.Serialize(u.ExternalId)}}}
              """);

        foreach (JsonProperty p in doc.RootElement.EnumerateObject())
            d[p.Name] = p.Value.Clone();

        return d;
    }

    private static bool ReadActive(IReadOnlyDictionary<string, JsonElement> next, bool fallback)
    {
        if (!next.TryGetValue("active", out JsonElement el))
            return fallback;

        return el.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(el.GetString(), out bool b) && b,
            _ => fallback
        };
    }

    private static string ReadString(IReadOnlyDictionary<string, JsonElement> next, string key, string fallback)
    {
        if (!next.TryGetValue(key, out JsonElement el) || el.ValueKind != JsonValueKind.String)
            return fallback;

        string v = el.GetString() ?? fallback;

        return string.IsNullOrWhiteSpace(v) ? fallback : v.Trim();
    }

    private static string? ReadOptionalString(IReadOnlyDictionary<string, JsonElement> next, string key, string? fallback)
    {
        if (!next.TryGetValue(key, out JsonElement el) || el.ValueKind == JsonValueKind.Null)
            return fallback;

        return el.ValueKind != JsonValueKind.String ? fallback : el.GetString();
    }

    private static string? TryReadOptionalTrimmed(IReadOnlyDictionary<string, JsonElement> next, string key, StringComparer comparer)
    {
        foreach (KeyValuePair<string, JsonElement> p in next)
        {
            if (comparer.Compare(p.Key, key) != 0)
                continue;

            JsonElement el = p.Value;

            if (el.ValueKind == JsonValueKind.Null || el.ValueKind != JsonValueKind.String)
                return null;

            string? trimmed = el.GetString()?.Trim();

            return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
        }

        return null;
    }

    private async Task<string?> ResolveRoleAsync(Guid tenantId, Guid? userId, CancellationToken ct)
    {
        if (userId is null)
            return null;

        IReadOnlyList<(string DisplayName, string ExternalId)> groups =
            await _users.ListGroupKeysForUserAsync(tenantId, userId.Value, ct);

        int best = 0;
        string? chosen = null;

        foreach ((string display, string external) in groups)
        {
            string? role = _roleMapper.TryMapGroupToRole(display, external);

            if (role is null)
                continue;

            int rank = RoleRank(role);

            if (rank <= best)
                continue;

            best = rank;
            chosen = role;
        }

        return chosen;
    }

    private static int RoleRank(string role) =>
        role.Trim() switch
        {
            "Admin" => 4,
            "Operator" => 3,
            "Auditor" => 2,
            "Reader" => 1,
            _ => 0
        };

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

    private static string JsonEncoded(string s) => JsonSerializer.Serialize(s).Trim('"');

    private static string? TryReadManualResolvedRoleFromUserResource(JsonElement resource)
    {
        if (!resource.TryGetProperty(ManualResolvedRoleFlatPath, out JsonElement el) ||
            el.ValueKind != JsonValueKind.String)
            return null;

        string? trimmed = el.GetString()?.Trim();

        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}

public sealed class ScimConflictException : Exception
{
    public ScimConflictException(string message)
        : base(message)
    {
    }
}

public sealed class ScimNotFoundException : Exception
{
    public ScimNotFoundException(string message)
        : base(message)
    {
    }
}
