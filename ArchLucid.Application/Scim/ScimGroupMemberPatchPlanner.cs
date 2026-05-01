using System.Text.Json;

using ArchLucid.Application.Scim.Patching;

namespace ArchLucid.Application.Scim;

internal static class ScimGroupMemberPatchPlanner
{
    internal static HashSet<Guid> ApplyOrderedOperations(JsonElement ops, HashSet<Guid> working)
    {
        foreach (JsonElement op in ops.EnumerateArray())
        {
            if (op.ValueKind != JsonValueKind.Object)
                throw new ScimUserResourceParseException("invalidSyntax", "Each operation must be a JSON object.");

            string? opName = op.TryGetProperty("op", out JsonElement on) && on.ValueKind == JsonValueKind.String ? on.GetString() : null;

            if (opName is null)
                throw new ScimUserResourceParseException("invalidSyntax", "Operation missing string 'op'.");

            if (!op.TryGetProperty("path", out JsonElement pathEl) || pathEl.ValueKind != JsonValueKind.String ||
                pathEl.GetString() is not { } pathRaw || string.IsNullOrWhiteSpace(pathRaw))
                throw new ScimUserResourceParseException("invalidPath", "Each group membership operation requires 'path'.");

            ScimPatchPathParseOutcome pathModel = ScimPatchValuePathParser.ParseForGroupMemberPath(pathRaw);

            switch (pathModel)
            {
                case ScimPatchPathInvalidOutcome invalid:
                    throw new ScimUserResourceParseException("invalidPath", invalid.Detail);

                case ScimPatchPathNotImplementedOutcome ni:
                    throw new ScimUserResourceParseException("notImplemented", ni.Detail);

                case ScimPatchFlatAttributePathOutcome:
                    ApplyBulkOp(working, opName, op);

                    continue;

                case ScimPatchMembersFilteredPathOutcome filtered:
                    ApplyFilteredOp(working, opName, op, filtered);

                    continue;

                default:
                    throw new ScimUserResourceParseException(
                        "notImplemented",
                        "Complex attribute path outcome is not implemented.");
            }
        }

        return working;
    }

    private static void ApplyBulkOp(HashSet<Guid> working, string opName, JsonElement op)
    {
        if (string.Equals(opName, "remove", StringComparison.OrdinalIgnoreCase))
        {
            working.Clear();

            return;
        }

        if (!op.TryGetProperty("value", out JsonElement val))
            throw new ScimUserResourceParseException("invalidValue", $"'{opName}' on 'members' requires 'value'.");

        List<Guid> ids = ExtractMemberUserIds(val);

        if (string.Equals(opName, "add", StringComparison.OrdinalIgnoreCase))
        {
            foreach (Guid id in ids)
                working.Add(id);

            return;
        }

        if (string.Equals(opName, "replace", StringComparison.OrdinalIgnoreCase))
        {
            working.Clear();

            foreach (Guid id in ids)
                working.Add(id);

            return;
        }

        throw new ScimUserResourceParseException("invalidSyntax", $"Unsupported op '{opName}'.");
    }

    private static void ApplyFilteredOp(
        HashSet<Guid> working,
        string opName,
        JsonElement op,
        ScimPatchMembersFilteredPathOutcome filtered)
    {
        Guid target = filtered.ReferenceUserId;

        if (filtered.SubAttribute is null)
            ApplyFilteredWholeMember(working, opName, op, target);

        else
            ApplyFilteredActive(working, opName, op, target);
    }

    private static void ApplyFilteredWholeMember(HashSet<Guid> working, string opName, JsonElement op, Guid target)
    {
        if (string.Equals(opName, "remove", StringComparison.OrdinalIgnoreCase))
        {
            working.Remove(target);

            return;
        }

        if (string.Equals(opName, "add", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(opName, "replace", StringComparison.OrdinalIgnoreCase))
            throw new ScimUserResourceParseException(
                "notImplemented",
                "add/replace with members[value eq \"…\"] selector is not implemented; use path 'members' with an array.");

        throw new ScimUserResourceParseException("invalidSyntax", $"Unsupported op '{opName}' for member filter path.");
    }

    private static void ApplyFilteredActive(HashSet<Guid> working, string opName, JsonElement op, Guid target)
    {
        if (string.Equals(opName, "remove", StringComparison.OrdinalIgnoreCase))
        {
            working.Remove(target);

            return;
        }

        if (!string.Equals(opName, "replace", StringComparison.OrdinalIgnoreCase))
            throw new ScimUserResourceParseException(
                "notImplemented",
                "Members '.active' sub-attribute supports 'replace' and 'remove' only.");

        if (!op.TryGetProperty("value", out JsonElement val))
            throw new ScimUserResourceParseException(
                "invalidValue",
                "replace members[…].active requires boolean 'value'.");

        bool active = ParseStrictBoolean(val);

        if (active)
            working.Add(target);
        else
            working.Remove(target);
    }

    private static bool ParseStrictBoolean(JsonElement val) =>
        val.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String =>
                bool.TryParse(val.GetString(), out bool b)
                ? b
                : throw new ScimUserResourceParseException(
                    "invalidValue",
                    "Members '.active' value must be a boolean."),
            _ => throw new ScimUserResourceParseException(
                "invalidValue",
                "Members '.active' value must be a boolean.")
        };

    private static List<Guid> ExtractMemberUserIds(JsonElement val)
    {
        if (val.ValueKind != JsonValueKind.Array)
            throw new ScimUserResourceParseException("invalidValue", "members bulk value must be a JSON array.");

        List<Guid> ids = [];

        foreach (JsonElement m in val.EnumerateArray())
        {
            if (m.TryGetProperty("value", out JsonElement idEl) &&
                idEl.ValueKind == JsonValueKind.String &&
                Guid.TryParse(idEl.GetString(), out Guid uid))
                ids.Add(uid);
        }

        return ids;
    }
}
