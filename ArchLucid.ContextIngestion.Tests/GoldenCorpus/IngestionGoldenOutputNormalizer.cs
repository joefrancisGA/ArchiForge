using System.Text.Json.Nodes;

using ArchLucid.TestSupport.GoldenCorpus;

namespace ArchLucid.ContextIngestion.Tests.GoldenCorpus;

/// <summary>Stable JSON for golden comparisons (property order, sorted nested dictionaries).</summary>
internal static class IngestionGoldenOutputNormalizer
{
    public static string Normalize(string json)
    {
        JsonNode? root = JsonNode.Parse(json);
        Assert.NotNull(root);
        if (root is JsonArray array)
        {
            JsonArray sorted = [];
            foreach (JsonNode? item in array)
            {
                if (item is not JsonObject obj)
                    continue;

                sorted.Add(SortObjectAndProps(obj));
            }

            return sorted.ToJsonString(GoldenCorpusJson.SerializerOptions);
        }

        return root.ToJsonString(GoldenCorpusJson.SerializerOptions);
    }

    private static JsonObject SortObjectAndProps(JsonObject o)
    {
        JsonObject copy = new();
        foreach (KeyValuePair<string, JsonNode?> kv in o.OrderBy(static x => x.Key, StringComparer.Ordinal))
        {
            if (kv.Key is "properties" && kv.Value is JsonObject p)
            {
                JsonObject sortedProps = new();
                foreach (KeyValuePair<string, JsonNode?> pkv in p.OrderBy(static x => x.Key, StringComparer.Ordinal))
                {
                    sortedProps[pkv.Key] = DeepCopy(pkv.Value);
                }

                copy[kv.Key] = sortedProps;
            }
            else

                copy[kv.Key] = DeepCopy(kv.Value);
        }

        return copy;
    }

    private static JsonNode? DeepCopy(JsonNode? node)
    {
        if (node is null)
            return null;

        return JsonNode.Parse(node.ToJsonString())!;
    }
}
