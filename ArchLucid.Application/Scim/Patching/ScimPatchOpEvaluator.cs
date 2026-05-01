using System.Text.Json;

namespace ArchLucid.Application.Scim.Patching;

/// <summary>
/// SCIM PATCH (RFC 7644 §3.5): flat paths for User resources; complex selectors on Users are <c>501 notImplemented</c>
/// upstream (parsed in <see cref="ScimPatchValuePathParser" />).
/// </summary>
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

        string rawPath = pathEl.GetString() ?? string.Empty;

        string path = ScimPatchValuePathParser.ParseForUserFlatPatchPath(rawPath);

        string key = path.Trim();

        if (string.IsNullOrEmpty(key))
            throw new ScimPatchException("invalidPath", "Empty path.");

        op.TryGetProperty("value", out JsonElement value);

        if (string.Equals(opName, "remove", StringComparison.OrdinalIgnoreCase))
        {
            target.Remove(key);

            return;
        }

        if (!string.Equals(opName, "add", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(opName, "replace", StringComparison.OrdinalIgnoreCase))
            throw new ScimPatchException("invalidSyntax", $"Unsupported op '{opName}'.");
        if (value.ValueKind == JsonValueKind.Undefined)
            throw new ScimPatchException("invalidValue", $"'{opName}' requires 'value'.");

        target[key] = value.Clone();
    }
}
