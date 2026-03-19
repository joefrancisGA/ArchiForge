using System.Text.Json;
using Json.More;
using Json.Schema;

namespace ArchiForge.Cli;

public static class ManifestValidator
{
    private static readonly JsonSerializerOptions s_jsonWriteIndented = new() { WriteIndented = true };
    public static void ValidateOrThrow(string schemaPath, string manifestPath)
    {
        if (!File.Exists(schemaPath)) throw new FileNotFoundException("Schema file not found.", schemaPath);
        if (!File.Exists(manifestPath)) throw new FileNotFoundException("Manifest file not found.", manifestPath);

        var schemaJson = File.ReadAllText(schemaPath);
        var manifestJson = File.ReadAllText(manifestPath);

        var schema = JsonSchema.FromText(schemaJson);

        using var manifestDoc = JsonDocument.Parse(manifestJson);

        var options = new EvaluationOptions
        {
            OutputFormat = OutputFormat.Hierarchical
        };

        var result = schema.Evaluate(manifestDoc.RootElement, options);

        if (!result.IsValid)
        {
            var pretty = JsonSerializer.Serialize(
                result.ToJsonDocument().RootElement,
                s_jsonWriteIndented);

            throw new InvalidDataException("Manifest validation failed:\n" + pretty);
        }
    }

    public static bool TryValidate(string schemaPath, string manifestJson, out string errorsJson)
    {
        errorsJson = "";
        var schema = JsonSchema.FromFile(schemaPath);
        using var doc = JsonDocument.Parse(manifestJson);

        var result = schema.Evaluate(doc.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });

        if (result.IsValid) return true;

        errorsJson = JsonSerializer.Serialize(
            result.ToJsonDocument().RootElement,
            s_jsonWriteIndented);

        return false;
    }
}
