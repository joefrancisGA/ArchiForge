using System.Reflection;
using System.Text.Json;

using ArchLucid.Core.Integration;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Integration;

/// <summary>Ensures <c>schemas/integration-events/catalog.json</c> stays aligned with <see cref="IntegrationEventTypes"/> and copied schema files.</summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class IntegrationEventCatalogSyncTests
{
    [Fact]
    public void Catalog_json_matches_integration_event_types_and_schema_artifacts()
    {
        string catalogPath = Path.Combine(AppContext.BaseDirectory, "schemas", "integration-events", "catalog.json");
        File.Exists(catalogPath).Should().BeTrue(because: $"catalog must be copied to test output: {catalogPath}");

        string catalogJson = File.ReadAllText(catalogPath);
        using JsonDocument catalogDoc = JsonDocument.Parse(catalogJson);
        JsonElement root = catalogDoc.RootElement;
        JsonElement eventsElement = root.GetProperty("events");

        List<(string EventType, string SchemaFile, string SchemaUri)> catalogRows = [];

        foreach (JsonElement eventElement in eventsElement.EnumerateArray())
        {
            string eventType = eventElement.GetProperty("eventType").GetString()!;
            string schemaFile = eventElement.GetProperty("schemaFile").GetString()!;
            string schemaUri = eventElement.GetProperty("schemaUri").GetString()!;
            string transport = eventElement.GetProperty("transport").GetString()!;
            bool outboxSupported = eventElement.GetProperty("outboxSupported").GetBoolean();

            transport.Should().Be("Azure Service Bus (topic)", because: $"catalog transport for {eventType} must match published integration channel");
            outboxSupported.Should().BeTrue(because: $"catalog outboxSupported for {eventType} must match transactional outbox coverage in docs");

            catalogRows.Add((eventType, schemaFile, schemaUri));
        }

        IReadOnlyList<string> codeEventTypes = GetPublishedIntegrationEventTypeStrings();

        catalogRows.Select(row => row.EventType).Should().BeEquivalentTo(
            codeEventTypes,
            because: "every IntegrationEventTypes constant (except wildcard) must appear in catalog, and catalog must not list unknown types");

        string schemasDir = Path.Combine(AppContext.BaseDirectory, "schemas", "integration-events");

        foreach ((string _, string schemaFile, string schemaUri) in catalogRows)
        {
            string schemaPath = Path.Combine(schemasDir, schemaFile);
            File.Exists(schemaPath).Should().BeTrue(because: $"catalog schemaFile must exist in test output: {schemaPath}");

            string schemaText = File.ReadAllText(schemaPath);
            using JsonDocument schemaDoc = JsonDocument.Parse(schemaText);
            JsonElement schemaRoot = schemaDoc.RootElement;

            schemaRoot.TryGetProperty("$id", out JsonElement idElement).Should().BeTrue(because: $"schema {schemaFile} must declare $id");
            string? idFromFile = idElement.GetString();
            idFromFile.Should().Be(schemaUri, because: "catalog schemaUri must match the schema file $id");
        }
    }

    private static IReadOnlyList<string> GetPublishedIntegrationEventTypeStrings()
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        List<string> values = typeof(IntegrationEventTypes)
            .GetFields(flags)
            .Where(field => field is { IsLiteral: true } && field.FieldType == typeof(string))
            .Where(field => field.Name != nameof(IntegrationEventTypes.WildcardEventType))
            .Select(field => (string)field.GetRawConstantValue()!)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        return values;
    }
}
