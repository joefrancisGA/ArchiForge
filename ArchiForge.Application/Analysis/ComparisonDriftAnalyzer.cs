using System.Text.Json;

namespace ArchiForge.Application.Analysis;

/// <summary>
/// Compares two object graphs by serializing them to JSON and performing a recursive field-level diff,
/// returning a <see cref="DriftAnalysisResult"/> that describes any detected changes.
/// </summary>
public sealed class ComparisonDriftAnalyzer : IComparisonDriftAnalyzer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    /// <inheritdoc />
    public DriftAnalysisResult Analyze(object stored, object regenerated)
    {
        var storedJson = JsonSerializer.SerializeToElement(stored, JsonOptions);
        var regeneratedJson = JsonSerializer.SerializeToElement(regenerated, JsonOptions);

        var result = new DriftAnalysisResult();

        CompareElement("$", storedJson, regeneratedJson, result.Items);

        result.DriftDetected = result.Items.Count > 0;

        result.Summary = result.DriftDetected
            ? $"{result.Items.Count} drift differences detected."
            : "No drift detected.";

        return result;
    }

    private static void CompareElement(
        string path,
        JsonElement left,
        JsonElement right,
        List<DriftItem> items)
    {
        if (left.ValueKind != right.ValueKind)
        {
            items.Add(new DriftItem
            {
                Category = "TypeChange",
                Path = path,
                StoredValue = left.ToString(),
                RegeneratedValue = right.ToString(),
                Description = "JSON value type changed."
            });

            return;
        }

        switch (left.ValueKind)
        {
            case JsonValueKind.Object:
                var leftProps = left.EnumerateObject().ToDictionary(p => p.Name);
                var rightProps = right.EnumerateObject().ToDictionary(p => p.Name);

                foreach (var prop in leftProps.Keys.Union(rightProps.Keys))
                {
                    leftProps.TryGetValue(prop, out var leftProp);
                    rightProps.TryGetValue(prop, out var rightProp);

                    if (!leftProps.ContainsKey(prop))
                    {
                        items.Add(new DriftItem
                        {
                            Category = "Added",
                            Path = $"{path}.{prop}",
                            RegeneratedValue = rightProp.Value.ToString(),
                            Description = "Property added."
                        });

                        continue;
                    }

                    if (!rightProps.ContainsKey(prop))
                    {
                        items.Add(new DriftItem
                        {
                            Category = "Removed",
                            Path = $"{path}.{prop}",
                            StoredValue = leftProp.Value.ToString(),
                            Description = "Property removed."
                        });

                        continue;
                    }

                    CompareElement($"{path}.{prop}", leftProp.Value, rightProp.Value, items);
                }

                break;

            case JsonValueKind.Array:
                var leftArray = left.EnumerateArray().ToList();
                var rightArray = right.EnumerateArray().ToList();

                if (leftArray.Count != rightArray.Count)
                {
                    items.Add(new DriftItem
                    {
                        Category = "ArrayLength",
                        Path = path,
                        StoredValue = leftArray.Count.ToString(),
                        RegeneratedValue = rightArray.Count.ToString(),
                        Description = "Array length changed."
                    });
                }

                for (int i = 0; i < Math.Min(leftArray.Count, rightArray.Count); i++)
                {
                    CompareElement($"{path}[{i}]", leftArray[i], rightArray[i], items);
                }

                break;

            default:
                if (left.ToString() != right.ToString())
                {
                    items.Add(new DriftItem
                    {
                        Category = "ValueChange",
                        Path = path,
                        StoredValue = left.ToString(),
                        RegeneratedValue = right.ToString(),
                        Description = "Value changed."
                    });
                }

                break;
        }
    }
}
