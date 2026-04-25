using System.Text.Json;

namespace ArchLucid.Application.Scim;

public static class ScimGroupResourceParser
{
    public static (string displayName, string externalId) ParseGroup(JsonElement resource)
    {
        if (resource.ValueKind != JsonValueKind.Object)
            throw new ScimUserResourceParseException("invalidSyntax", "Group resource must be a JSON object.");

        string displayName = ReadRequiredString(resource, "displayName");
        string externalId = ReadOptionalString(resource, "externalId") ?? displayName;

        return (displayName, externalId);
    }

    private static string ReadRequiredString(JsonElement resource, string name)
    {
        if (!resource.TryGetProperty(name, out JsonElement el) || el.ValueKind != JsonValueKind.String)
            throw new ScimUserResourceParseException("invalidValue", $"Missing or invalid '{name}'.");

        string v = el.GetString() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(v))
            throw new ScimUserResourceParseException("invalidValue", $"'{name}' must be non-empty.");

        return v.Trim();
    }

    private static string? ReadOptionalString(JsonElement resource, string name)
    {
        if (!resource.TryGetProperty(name, out JsonElement el))
            return null;

        if (el.ValueKind == JsonValueKind.Null)
            return null;

        if (el.ValueKind != JsonValueKind.String)
            throw new ScimUserResourceParseException("invalidValue", $"'{name}' must be a string.");

        return el.GetString();
    }
}
