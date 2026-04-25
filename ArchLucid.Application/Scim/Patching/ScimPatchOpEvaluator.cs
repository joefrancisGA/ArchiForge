using System.Text.Json;

namespace ArchLucid.Application.Scim.Patching;

/// <summary>SCIM PATCH (RFC 7644) for flat attribute paths only (v1 rejects complex selectors).</summary>
public static class ScimPatchOpEvaluator
{
    public static IReadOnlyDictionary<string, JsonElement> ApplyFlat(
        IReadOnlyDictionary<string, JsonElement> current,
        JsonElement patchDocument)
    {
        if (patchDocument.ValueKind != JsonValueKind.Object)
            throw new ScimPatchException("invalidSyntax", "PATCH body must be a JSON object.");

        if (!patchDocument.TryGetProperty("Operations", out JsonElement ops) || ops.ValueKind != JsonValueKind.Array)
            throw new ScimPatchException("invalidSyntax", "Missing or invalid 'Operations' array.");

        Dictionary<string, JsonElement> next = new(current, StringComparer.OrdinalIgnoreCase);

        foreach (JsonElement op in ops.EnumerateArray())
            ApplyOne(next, op);

        return next;
    }

    private static void ApplyOne(Dictionary<string, JsonElement> target, JsonElement op)
    {
        if (op.ValueKind != JsonValueKind.Object)
            throw new ScimPatchException("invalidSyntax", "Each operation must be an object.");

        if (!op.TryGetProperty("op", out JsonElement opNameEl) || opNameEl.ValueKind != JsonValueKind.String)
            throw new ScimPatchException("invalidSyntax", "Operation missing string 'op'.");

        string opName = opNameEl.GetString() ?? string.Empty;

        if (!op.TryGetProperty("path", out JsonElement pathEl) || pathEl.ValueKind != JsonValueKind.String)
            throw new ScimPatchException("invalidPath", "Flat PATCH requires a string 'path' per operation.");

        string path = pathEl.GetString() ?? string.Empty;

        if (path.Contains('\"', StringComparison.Ordinal) || path.Contains('[', StringComparison.Ordinal) ||
            path.Contains('(', StringComparison.Ordinal))
            throw new ScimPatchException("invalidPath", "Complex SCIM selectors are not supported in v1.");

        string key = path.Trim();

        if (string.IsNullOrEmpty(key))
            throw new ScimPatchException("invalidPath", "Empty path.");

        op.TryGetProperty("value", out JsonElement value);

        if (string.Equals(opName, "remove", StringComparison.OrdinalIgnoreCase))
        {
            target.Remove(key);

            return;
        }

        if (string.Equals(opName, "add", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(opName, "replace", StringComparison.OrdinalIgnoreCase))
        {
            if (value.ValueKind == JsonValueKind.Undefined)
                throw new ScimPatchException("invalidValue", $"'{opName}' requires 'value'.");

            target[key] = value.Clone();

            return;
        }

        throw new ScimPatchException("invalidSyntax", $"Unsupported op '{opName}'.");
    }
}
