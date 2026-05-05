using System.Text.Json;

using ArchLucid.Core.Integration;

using FluentAssertions;

namespace ArchLucid.Application.Tests.Integration;

/// <summary>
///     Ensures UTF-8 bodies produced with <see cref="IntegrationEventJson.Options" /> match
///     <c>schemas/integration-events/*.v1.schema.json</c>. Production code publishes via outbox / Service Bus (there is
///     no single "dispatcher" type; samples mirror emitted shapes).
/// </summary>
[Trait("Suite", "Application")]
[Trait("Category", "Unit")]
public sealed class IntegrationEventWebhookPayloadJsonSchemaTests
{
    public static IEnumerable<object[]> CatalogSchemaFiles()
    {
        string catalogPath = Path.Combine(
            AppContext.BaseDirectory,
            "schemas",
            "integration-events",
            "catalog.json");

        File.Exists(catalogPath).Should().BeTrue("catalog must be copied to test output: {0}", catalogPath);

        string catalogJson = File.ReadAllText(catalogPath);

        using JsonDocument catalogDoc = JsonDocument.Parse(catalogJson);

        foreach (JsonElement entry in catalogDoc.RootElement.GetProperty("events").EnumerateArray())
        {
            string schemaFile = entry.GetProperty("schemaFile").GetString()!;

            yield return [schemaFile];
        }
    }

    [Theory]
    [MemberData(nameof(CatalogSchemaFiles))]
    public void Catalog_event_payload_sample_validates_against_schema(string schemaFile)
    {
        object sample = IntegrationEventWebhookPayloadSampleFactory.Create(schemaFile);

        byte[] utf8 = JsonSerializer.SerializeToUtf8Bytes(sample, IntegrationEventJson.Options);

        IntegrationEventJsonSchemaAssert.Utf8PayloadValidates(schemaFile, utf8);
    }
}
