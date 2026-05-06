using System.Text.Json;

namespace ArchLucid.Application.Analysis;
/// <summary>
///     Compares two object graphs by serializing them to JSON and performing a recursive field-level diff,
///     returning a <see cref="DriftAnalysisResult"/> that describes any detected changes.
/// </summary>
public sealed class ComparisonDriftAnalyzer : IComparisonDriftAnalyzer
{
    // Use CLR property names in serialized JSON so drift paths match model members (e.g. $.Outer.Inner),
    // not camelCase wire format. Both sides use the same options, so comparison stays consistent.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };
    /// <inheritdoc/>
    public DriftAnalysisResult Analyze(object stored, object regenerated)
    {
        ArgumentNullException.ThrowIfNull(stored);
        ArgumentNullException.ThrowIfNull(regenerated);
        JsonElement storedJson = JsonSerializer.SerializeToElement(stored, JsonOptions);
        JsonElement regeneratedJson = JsonSerializer.SerializeToElement(regenerated, JsonOptions);
        DriftAnalysisResult result = new();
        CompareElement("$", storedJson, regeneratedJson, result.Items);
        result.DriftDetected = result.Items.Count > 0;
        result.Summary = result.DriftDetected ? $"{result.Items.Count} drift differences detected." : "No drift detected.";
        return result;
    }

    private static void CompareElement(string path, JsonElement left, JsonElement right, List<DriftItem> items)
    {
        if (left.ValueKind != right.ValueKind)
        {
            items.Add(new DriftItem { Category = "TypeChange", Path = path, StoredValue = left.ToString(), RegeneratedValue = right.ToString(), Description = "JSON value type changed." });
            return;
        }

        switch (left.ValueKind)
        {
            case JsonValueKind.Object:
                // GroupBy guards against malformed JSON with duplicate property names;
                // first occurrence wins, matching System.Text.Json's own lenient behaviour.
                Dictionary<string, JsonProperty> leftProps = left.EnumerateObject().GroupBy(p => p.Name, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
                Dictionary<string, JsonProperty> rightProps = right.EnumerateObject().GroupBy(p => p.Name, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
                foreach (string prop in leftProps.Keys.Union(rightProps.Keys))
                {
                    leftProps.TryGetValue(prop, out JsonProperty leftProp);
                    rightProps.TryGetValue(prop, out JsonProperty rightProp);
                    if (!leftProps.ContainsKey(prop))
                    {
                        items.Add(new DriftItem { Category = "Added", Path = $"{path}.{prop}", RegeneratedValue = rightProp.Value.ToString(), Description = "Property added." });
                        continue;
                    }

                    if (!rightProps.ContainsKey(prop))
                    {
                        items.Add(new DriftItem { Category = "Removed", Path = $"{path}.{prop}", StoredValue = leftProp.Value.ToString(), Description = "Property removed." });
                        continue;
                    }

                    CompareElement($"{path}.{prop}", leftProp.Value, rightProp.Value, items);
                }

                break;
            case JsonValueKind.Array:
                List<JsonElement> leftArray = left.EnumerateArray().ToList();
                List<JsonElement> rightArray = right.EnumerateArray().ToList();
                if (leftArray.Count != rightArray.Count)
                    items.Add(new DriftItem { Category = "ArrayLength", Path = path, StoredValue = leftArray.Count.ToString(), RegeneratedValue = rightArray.Count.ToString(), Description = "Array length changed." });
                for (int i = 0; i < Math.Min(leftArray.Count, rightArray.Count); i++)
                    CompareElement($"{path}[{i}]", leftArray[i], rightArray[i], items);
                break;
            default:
                if (left.ToString() != right.ToString())
                    items.Add(new DriftItem { Category = "ValueChange", Path = path, StoredValue = left.ToString(), RegeneratedValue = right.ToString(), Description = "Value changed." });
                break;
        }
    }
}