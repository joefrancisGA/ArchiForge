using System.Text.Json;

using ArchLucid.Core.Integration;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Integration;

/// <summary>Guards JSON shapes for outbound integration events (additive fields allowed; required names/types should stay stable).</summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class IntegrationEventPayloadContractTests
{
    [Fact]
    public void AuthorityRunCompleted_payload_has_expected_contract()
    {
        object payload = new
        {
            schemaVersion = 1,
            runId = Guid.NewGuid(),
            manifestId = Guid.NewGuid(),
            tenantId = Guid.NewGuid(),
            workspaceId = Guid.NewGuid(),
            projectId = Guid.NewGuid(),
        };

        AssertPayloadMatchesCommittedSchema("authority-run-completed.v1.schema.json", payload);
    }

    [Fact]
    public void GovernanceApprovalSubmitted_payload_has_expected_contract()
    {
        object payload = new
        {
            schemaVersion = 1,
            tenantId = Guid.NewGuid(),
            workspaceId = Guid.NewGuid(),
            projectId = Guid.NewGuid(),
            approvalRequestId = "a",
            runId = "r",
            manifestVersion = "v",
            sourceEnvironment = "dev",
            targetEnvironment = "test",
            requestedBy = "u",
        };

        AssertPayloadMatchesCommittedSchema("governance-approval-submitted.v1.schema.json", payload);
    }

    [Fact]
    public void GovernancePromotionActivated_payload_has_expected_contract()
    {
        object payload = new
        {
            schemaVersion = 1,
            tenantId = Guid.NewGuid(),
            workspaceId = Guid.NewGuid(),
            projectId = Guid.NewGuid(),
            activationId = "x",
            runId = "r",
            manifestVersion = "v",
            environment = "prod",
            activatedBy = "u",
            activatedUtc = DateTime.UtcNow,
        };

        AssertPayloadMatchesCommittedSchema("governance-promotion-activated.v1.schema.json", payload);
    }

    [Fact]
    public void AlertFired_payload_has_expected_contract()
    {
        object payload = new
        {
            schemaVersion = 1,
            tenantId = Guid.NewGuid(),
            workspaceId = Guid.NewGuid(),
            projectId = Guid.NewGuid(),
            alertId = Guid.NewGuid(),
            runId = (Guid?)Guid.NewGuid(),
            comparedToRunId = (Guid?)null,
            ruleId = "rule",
            category = "c",
            severity = "High",
            title = "t",
            deduplicationKey = "k",
        };

        AssertPayloadMatchesCommittedSchema("alert-fired.v1.schema.json", payload);
    }

    [Fact]
    public void AlertResolved_payload_has_expected_contract()
    {
        object payload = new
        {
            schemaVersion = 1,
            tenantId = Guid.NewGuid(),
            workspaceId = Guid.NewGuid(),
            projectId = Guid.NewGuid(),
            alertId = Guid.NewGuid(),
            runId = (Guid?)null,
            resolvedByUserId = "u",
            comment = (string?)null,
        };

        AssertPayloadMatchesCommittedSchema("alert-resolved.v1.schema.json", payload);
    }

    [Fact]
    public void AdvisoryScanCompleted_payload_has_expected_contract()
    {
        object payload = new
        {
            schemaVersion = 1,
            tenantId = Guid.NewGuid(),
            workspaceId = Guid.NewGuid(),
            projectId = Guid.NewGuid(),
            scheduleId = Guid.NewGuid(),
            executionId = Guid.NewGuid(),
            hasRuns = true,
            runId = (Guid?)null,
            comparedToRunId = (Guid?)null,
            digestId = (Guid?)null,
            completedUtc = DateTime.UtcNow,
        };

        AssertPayloadMatchesCommittedSchema("advisory-scan-completed.v1.schema.json", payload);
    }

    private static void AssertPayloadMatchesCommittedSchema(string schemaFileName, object payload)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "schemas", "integration-events", schemaFileName);

        File.Exists(path).Should().BeTrue(because: $"schema file must be copied to test output: {path}");

        string schemaJson = File.ReadAllText(path);
        using JsonDocument schemaDoc = JsonDocument.Parse(schemaJson);
        JsonElement schemaRoot = schemaDoc.RootElement;

        byte[] utf8 = JsonSerializer.SerializeToUtf8Bytes(payload, IntegrationEventJson.Options);

        using JsonDocument payloadDoc = JsonDocument.Parse(utf8);
        JsonElement payloadRoot = payloadDoc.RootElement;

        payloadRoot.ValueKind.Should().Be(JsonValueKind.Object);

        if (schemaRoot.TryGetProperty("required", out JsonElement requiredElement)
            && requiredElement.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement nameElement in requiredElement.EnumerateArray())
            {
                string? name = nameElement.GetString();

                name.Should().NotBeNullOrWhiteSpace();

                payloadRoot.TryGetProperty(name!, out JsonElement _).Should().BeTrue(
                    because: $"required property '{name}' must exist in serialized payload for {schemaFileName}");
            }
        }

        if (schemaRoot.TryGetProperty("properties", out JsonElement propertiesElement)
            && propertiesElement.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty payloadProperty in payloadRoot.EnumerateObject())
            {
                propertiesElement.TryGetProperty(payloadProperty.Name, out JsonElement _).Should().BeTrue(
                    because:
                    $"serialized payload property '{payloadProperty.Name}' must be declared in schema {schemaFileName} (catch typos; additional payload fields require a schema update)");
            }
        }
    }
}
