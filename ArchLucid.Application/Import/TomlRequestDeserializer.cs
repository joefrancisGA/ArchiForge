using System.Globalization;
using System.Text.Json;
using ArchLucid.Contracts.Requests;
using Tomlyn;
using Tomlyn.Model;

namespace ArchLucid.Application.Import;
/// <summary>
///     Strict TOML import: parse to <see cref="TomlTable"/>, convert to JSON, then
///     <see cref="JsonRequestDeserializer"/> (unknown fields rejected).
/// </summary>
public static class TomlRequestDeserializer
{
    private const int MaxDepth = 5;
    public static ArchitectureRequest Deserialize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text is null)
            throw new ArgumentNullException(nameof(text));
        TomlTable model;
        try
        {
            model = Toml.ToModel<TomlTable>(text);
        }
        catch (TomlException ex)
        {
            throw new InvalidOperationException($"Malformed TOML: {ex.Message}", ex);
        }

        JsonElement json = TomlTableToJsonElement(model, 0);
        string jsonText = json.GetRawText();
        return JsonRequestDeserializer.DeserializeText(jsonText);
    }

    private static JsonElement TomlTableToJsonElement(TomlTable table, int depth)
    {
        if (depth > MaxDepth)
            throw new InvalidOperationException($"TOML nesting exceeds maximum depth ({MaxDepth}).");
        Dictionary<string, JsonElement> props = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, object> kv in table)
        {
            if (string.IsNullOrWhiteSpace(kv.Key))
                continue;
            string name = Naming.ToCamelCase(kv.Key.Trim());
            props[name] = TomlValueToJson(kv.Value, depth + 1);
        }

        return JsonSerializer.SerializeToElement(props, ImportArchitectureRequestSerializerOptions.StrictDeserialize);
    }

    private static JsonElement TomlValueToJson(object? value, int depth)
    {
        if (value is null)
            return JsonSerializer.SerializeToElement<object?>(null, ImportArchitectureRequestSerializerOptions.StrictDeserialize);
        switch (value)
        {
            case string s:
                return JsonSerializer.SerializeToElement(s, ImportArchitectureRequestSerializerOptions.StrictDeserialize);
            case bool b:
                return JsonSerializer.SerializeToElement(b, ImportArchitectureRequestSerializerOptions.StrictDeserialize);
            case int i:
                return JsonSerializer.SerializeToElement(i, ImportArchitectureRequestSerializerOptions.StrictDeserialize);
            case long l:
                return JsonSerializer.SerializeToElement(l, ImportArchitectureRequestSerializerOptions.StrictDeserialize);
            case double d:
                return JsonSerializer.SerializeToElement(d, ImportArchitectureRequestSerializerOptions.StrictDeserialize);
            case DateTimeOffset dto:
                return JsonSerializer.SerializeToElement(dto, ImportArchitectureRequestSerializerOptions.StrictDeserialize);
            case DateTime dt:
                return JsonSerializer.SerializeToElement(dt, ImportArchitectureRequestSerializerOptions.StrictDeserialize);
            case TomlTable nested:
                return TomlTableToJsonElement(nested, depth);
            case TomlArray arr:
                if (depth > MaxDepth)
                    throw new InvalidOperationException($"TOML nesting exceeds maximum depth ({MaxDepth}).");
                List<JsonElement> items = [];
                foreach (object? item in arr)
                    items.Add(TomlValueToJson(item, depth));
                return JsonSerializer.SerializeToElement(items, ImportArchitectureRequestSerializerOptions.StrictDeserialize);
            default:
                return JsonSerializer.SerializeToElement(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty, ImportArchitectureRequestSerializerOptions.StrictDeserialize);
        }
    }

    private static class Naming
    {
        public static string ToCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;
            // snake_case → camelCase for typical TOML keys
            if (!name.Contains('_', StringComparison.Ordinal))
            {
                if (char.IsUpper(name[0]))
                    return char.ToLowerInvariant(name[0]) + name[1..];
                return name;
            }

            string[] parts = name.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
                return name;
            List<string> merged = [];
            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i];
                if (p.Length == 0)
                    continue;
                if (i == 0)
                {
                    merged.Add(char.ToLowerInvariant(p[0]) + p[1..]);
                    continue;
                }

                merged.Add(char.ToUpperInvariant(p[0]) + p[1..]);
            }

            return string.Concat(merged);
        }
    }
}