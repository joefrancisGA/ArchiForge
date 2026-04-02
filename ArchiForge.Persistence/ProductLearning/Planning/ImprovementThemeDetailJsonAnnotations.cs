using System.Text.Json;

namespace ArchiForge.Persistence.ProductLearning.Planning;

/// <summary>
/// Reads simple string tags from <c>ProductLearningPilotSignals.DetailJson</c> (no schema registry; invalid JSON yields empty).
/// </summary>
internal static class ImprovementThemeDetailJsonAnnotations
{
    internal static IReadOnlyList<string> ReadAnnotationTokens(string? detailJson)
    {
        if (string.IsNullOrWhiteSpace(detailJson))
        {
            return Array.Empty<string>();
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(detailJson);
            JsonElement root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return Array.Empty<string>();
            }

            HashSet<string> seen = new(StringComparer.Ordinal);
            List<string> ordered = new();

            AddPropertyTokens(root, "tags", seen, ordered);
            AddPropertyTokens(root, "annotations", seen, ordered);
            AddPropertyTokens(root, "tag", seen, ordered);
            AddPropertyTokens(root, "annotation", seen, ordered);

            return ordered;
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    internal static string NormalizeAnnotationToken(string token)
    {
        return token.Trim().ToLowerInvariant();
    }

    private static void AddPropertyTokens(
        JsonElement root,
        string propertyName,
        HashSet<string> seen,
        List<string> ordered)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement element))
        {
            return;
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            AddOneString(element, seen, ordered);

            return;
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (JsonElement item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            AddOneString(item, seen, ordered);
        }
    }

    private static void AddOneString(JsonElement stringElement, HashSet<string> seen, List<string> ordered)
    {
        string? value = stringElement.GetString();

        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        string trimmed = value.Trim();

        if (!seen.Add(trimmed))
        {
            return;
        }

        ordered.Add(trimmed);
    }
}
