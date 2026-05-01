using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

using ArchLucid.Core.Integration;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Integration;

/// <summary>
///     Ensures <c>schemas/integration-events/catalog.json</c> stays aligned with <see cref="IntegrationEventTypes" />
///     and copied schema files.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class IntegrationEventCatalogSyncTests
{
    [Fact]
    public void Catalog_json_matches_integration_event_types_and_schema_artifacts()
    {
        string catalogPath = Path.Combine(AppContext.BaseDirectory, "schemas", "integration-events", "catalog.json");
        File.Exists(catalogPath).Should().BeTrue($"catalog must be copied to test output: {catalogPath}");

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

            bool isInternal = eventElement.TryGetProperty("internal", out JsonElement internalElement)
                && internalElement.ValueKind is JsonValueKind.True;

            if (isInternal)
            {
                eventElement.TryGetProperty("audience", out _).Should().BeFalse(
                    $"internal catalog entry {eventType} must not set audience (reserved for external-only rows)");
            }
            else
            {
                eventElement.TryGetProperty("audience", out JsonElement audienceElement).Should().BeTrue(
                    $"non-internal catalog entry {eventType} must declare audience");
                audienceElement.GetString().Should().Be("external");
            }

            transport.Should().Be("Azure Service Bus (topic)",
                $"catalog transport for {eventType} must match published integration channel");

            if (string.Equals(eventType, IntegrationEventTypes.DataConsistencyCheckCompletedV1, StringComparison.Ordinal))
            {
                // Published via best-effort IIntegrationEventPublisher only (not the transactional outbox table).
                outboxSupported.Should().BeFalse(
                    $"catalog outboxSupported for {eventType} must match code path (direct publish after reconciliation)");
            }
            else
            {
                outboxSupported.Should()
                    .BeTrue($"catalog outboxSupported for {eventType} must match transactional outbox coverage in docs");
            }

            catalogRows.Add((eventType, schemaFile, schemaUri));
        }

        IReadOnlyList<string> codeEventTypes = GetPublishedIntegrationEventTypeStrings();

        catalogRows.Select(row => row.EventType).Should().BeEquivalentTo(
            codeEventTypes,
            "every IntegrationEventTypes constant (except wildcard) must appear in catalog, and catalog must not list unknown types");

        string schemasDir = Path.Combine(AppContext.BaseDirectory, "schemas", "integration-events");

        foreach ((string _, string schemaFile, string schemaUri) in catalogRows)
        {
            string schemaPath = Path.Combine(schemasDir, schemaFile);
            File.Exists(schemaPath).Should().BeTrue($"catalog schemaFile must exist in test output: {schemaPath}");

            string schemaText = File.ReadAllText(schemaPath);
            using JsonDocument schemaDoc = JsonDocument.Parse(schemaText);
            JsonElement schemaRoot = schemaDoc.RootElement;

            schemaRoot.TryGetProperty("$id", out JsonElement idElement).Should()
                .BeTrue($"schema {schemaFile} must declare $id");
            string? idFromFile = idElement.GetString();
            idFromFile.Should().Be(schemaUri, "catalog schemaUri must match the schema file $id");
        }
    }

    [Fact]
    public void Internal_catalog_event_types_are_absent_from_external_event_catalog_section_in_docs()
    {
        string catalogPath = Path.Combine(AppContext.BaseDirectory, "schemas", "integration-events", "catalog.json");
        string catalogJson = File.ReadAllText(catalogPath);
        using JsonDocument catalogDoc = JsonDocument.Parse(catalogJson);

        List<string> internalEventTypes = [];
        foreach (JsonElement eventElement in catalogDoc.RootElement.GetProperty("events").EnumerateArray())
        {
            bool isInternal = eventElement.TryGetProperty("internal", out JsonElement internalElement)
                && internalElement.ValueKind is JsonValueKind.True;

            if (!isInternal) continue;

            string eventType = eventElement.GetProperty("eventType").GetString()!;
            internalEventTypes.Add(eventType);
        }

        internalEventTypes.Should().NotBeEmpty("catalog must mark at least one internal dispatch event (trial email)");

        string docPath = Path.Combine(AppContext.BaseDirectory, "docs", "library", "INTEGRATION_EVENTS_AND_WEBHOOKS.md");
        File.Exists(docPath).Should().BeTrue($"doc must be copied to test output: {docPath}");

        string docText = File.ReadAllText(docPath);
        const string sectionHeading = "### Event catalog (canonical types)";
        int sectionStart = docText.IndexOf(sectionHeading, StringComparison.Ordinal);
        sectionStart.Should().BeGreaterThan(-1);

        int contentStart = sectionStart + sectionHeading.Length;
        int nextHeading = docText.IndexOf("\n### ", contentStart, StringComparison.Ordinal);
        string sectionBody = nextHeading < 0
            ? docText[contentStart..]
            : docText[contentStart..nextHeading];

        MatchCollection matches = Regex.Matches(sectionBody, @"(?m)^\d+\.\s+\*\*`([^`]+)`\*\*", RegexOptions.None);

        matches.Should().NotBeEmpty("external event catalog section must list canonical types as a numbered list");

        IReadOnlyCollection<string> externalListedTypes = matches
            .Select(m => m.Groups[1].Value.Trim())
            .ToList();

        foreach (string internalType in internalEventTypes)
        {
            externalListedTypes.Should().NotContain(internalType,
                $"internal-only integration type must not appear as a numbered external catalog row in INTEGRATION_EVENTS_AND_WEBHOOKS.md (section: {sectionHeading})");
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
