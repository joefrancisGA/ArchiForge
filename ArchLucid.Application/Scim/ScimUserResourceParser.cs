using System.Text.Json;

namespace ArchLucid.Application.Scim;

public static class ScimUserResourceParser
{
    public static (string userName, string? displayName, bool active, string externalId) ParseUser(JsonElement resource)
    {
        if (resource.ValueKind != JsonValueKind.Object)
            throw new ScimUserResourceParseException("invalidSyntax", "User resource must be a JSON object.");

        string userName = ReadRequiredString(resource, "userName");
        string? displayName = ReadOptionalString(resource, "displayName");
        bool active = ReadActive(resource);
        string externalId = ReadOptionalString(resource, "externalId") ?? userName;

        return (userName, displayName, active, externalId);
    }

    private static string ReadRequiredString(JsonElement resource, string name)
    {
        if (!resource.TryGetProperty(name, out JsonElement el) || el.ValueKind != JsonValueKind.String)
            throw new ScimUserResourceParseException("invalidValue", $"Missing or invalid '{name}'.");

        string v = el.GetString() ?? string.Empty;

        return string.IsNullOrWhiteSpace(v) ? throw new ScimUserResourceParseException("invalidValue", $"'{name}' must be non-empty.") : v.Trim();
    }

    private static string? ReadOptionalString(JsonElement resource, string name)
    {
        if (!resource.TryGetProperty(name, out JsonElement el))
            return null;

        if (el.ValueKind == JsonValueKind.Null)
            return null;

        return el.ValueKind != JsonValueKind.String ? throw new ScimUserResourceParseException("invalidValue", $"'{name}' must be a string.") : el.GetString();
    }

    private static bool ReadActive(JsonElement resource)
    {
        if (!resource.TryGetProperty("active", out JsonElement el))
            return true;

        return el.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(el.GetString(), out bool b) && b,
            _ => throw new ScimUserResourceParseException("invalidValue", "'active' must be a boolean.")
        };
    }
}

public sealed class ScimUserResourceParseException : Exception
{
    public ScimUserResourceParseException(string scimType, string detail)
        : base(detail)
    {
        ScimType = scimType;
    }

    public string ScimType
    {
        get;
    }
}
