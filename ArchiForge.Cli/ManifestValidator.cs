using System.Text.Json;

using Json.More;
using Json.Schema;

namespace ArchiForge.Cli;

public static class ManifestValidator
{
    private static readonly JsonSerializerOptions SJsonWriteIndented = new() { WriteIndented = true };
    public static void ValidateOrThrow(string schemaPath, string manifestPath)
    {
        if (!File.Exists(schemaPath))
            throw new FileNotFoundException("Schema file not found.", schemaPath);
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException("Manifest file not found.", manifestPath);

        string schemaJson = File.ReadAllText(schemaPath);
        string manifestJson = File.ReadAllText(manifestPath);

        JsonSchema schema = JsonSchema.FromText(schemaJson);

        using JsonDocument manifestDoc = JsonDocument.Parse(manifestJson);

        EvaluationOptions options = new()
        {
            OutputFormat = OutputFormat.Hierarchical
        };

        EvaluationResults result = schema.Evaluate(manifestDoc.RootElement, options);

        if (!result.IsValid)
        {
            string pretty = JsonSerializer.Serialize(
                result.ToJsonDocument().RootElement,
                SJsonWriteIndented);

            throw new InvalidDataException("Manifest validation failed:\n" + pretty);
        }
    }

    public static bool TryValidate(string schemaPath, string manifestJson, out string errorsJson)
    {
        errorsJson = "";
        JsonSchema schema = JsonSchema.FromFile(schemaPath);
        using JsonDocument doc = JsonDocument.Parse(manifestJson);

        EvaluationResults result = schema.Evaluate(doc.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });

        if (result.IsValid)
            return true;

        errorsJson = JsonSerializer.Serialize(
            result.ToJsonDocument().RootElement,
            SJsonWriteIndented);

        return false;
    }
}
