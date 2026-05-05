using System.Text.Json;

using FluentAssertions;

using Json.More;
using Json.Schema;

namespace ArchLucid.Application.Tests.Integration;

/// <summary>Strict JSON Schema validation using JsonSchema.Net (draft 2020-12), matching CLI manifest validation.</summary>
internal static class IntegrationEventJsonSchemaAssert
{
    private static readonly JsonSerializerOptions PrettyJson = new() { WriteIndented = true };

    internal static void Utf8PayloadValidates(string schemaFileName, ReadOnlySpan<byte> payloadUtf8)
    {
        if (string.IsNullOrWhiteSpace(schemaFileName))
            throw new ArgumentException("Schema file name is required.", nameof(schemaFileName));

        string schemaPath = Path.Combine(AppContext.BaseDirectory, "schemas", "integration-events", schemaFileName);

        File.Exists(schemaPath).Should().BeTrue("schema file must be copied to test output: {0}", schemaPath);

        string schemaText = File.ReadAllText(schemaPath);
        JsonSchema schema = JsonSchema.FromText(schemaText);

        ReadOnlyMemory<byte> jsonMemory = payloadUtf8.ToArray();

        using JsonDocument payloadDoc = JsonDocument.Parse(jsonMemory);

        EvaluationOptions evaluationOptions = new() { OutputFormat = OutputFormat.Hierarchical };

        EvaluationResults result = schema.Evaluate(payloadDoc.RootElement, evaluationOptions);

        if (result.IsValid)
            return;

        string detail = JsonSerializer.Serialize(payloadDoc.RootElement, PrettyJson);
        string errors = JsonSerializer.Serialize(result.ToJsonDocument().RootElement, PrettyJson);

        result.IsValid.Should().BeTrue(
            "payload for {0} must validate. Payload:\n{1}\nEvaluation:\n{2}",
            schemaFileName,
            detail,
            errors);
    }
}
