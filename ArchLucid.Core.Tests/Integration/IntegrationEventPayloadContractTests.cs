using System.Text.Json;

using ArchLucid.Core.Integration;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Integration;

/// <summary>
///     Guards JSON shapes for outbound integration events (additive fields allowed; required names/types should stay
///     stable).
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class IntegrationEventPayloadContractTests
{
    [Fact]
    public void AuthorityRunCompleted_payload_has_expected_contract()
    {
        Guid runId = Guid.NewGuid();

        object payload = new
        {
            schemaVersion = 1,
            runId,
            manifestId = Guid.NewGuid(),
            tenantId = Guid.NewGuid(),
            workspaceId = Guid.NewGuid(),
            projectId = Guid.NewGuid(),
            findings = new[]
            {
                new
                {
                    findingId = "finding-primary",
                    deepLinkUrl = new Uri($"https://archlucid.net/runs/{runId:D}/findings/finding-primary")
                        .ToString()
                }
            }
        };

        AssertPayloadMatchesCommittedSchema("authority-run-completed.v1.schema.json", payload);
    }

    [Fact]
    public void ManifestFinalized_payload_has_expected_contract()
    {
        object payload = new
        {
            schemaVersion = 1,
            runId = Guid.NewGuid(),
            manifestId = Guid.NewGuid(),
            decisionTraceId = Guid.NewGuid(),
            tenantId = Guid.NewGuid(),
            workspaceId = Guid.NewGuid(),
            projectId = Guid.NewGuid(),
            findingsSnapshotId = Guid.NewGuid(),
            artifactBundleId = Guid.NewGuid(),
            manifestVersion = "v1"
        };

        AssertPayloadMatchesCommittedSchema("manifest-finalized.v1.schema.json", payload);
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
            requestedBy = "u"
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
            activatedUtc = DateTime.UtcNow
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
            deduplicationKey = "k"
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
            deduplicationKey = "composite:rule:scope",
            resolvedByUserId = "u",
            comment = (string?)null
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
            completedUtc = DateTime.UtcNow
        };

        AssertPayloadMatchesCommittedSchema("advisory-scan-completed.v1.schema.json", payload);
    }

    [Fact]
    public void ComplianceDriftEscalated_payload_has_expected_contract()
    {
        object payload = new
        {
            schemaVersion = 1,
            tenantId = Guid.NewGuid(),
            workspaceId = Guid.NewGuid(),
            projectId = Guid.NewGuid(),
            driftSignalId = Guid.NewGuid(),
            escalatedUtc = DateTime.UtcNow,
            metricKey = "policyPackStaleHours",
            thresholdValue = 72.0,
            observedValue = 96.0
        };

        AssertPayloadMatchesCommittedSchema("compliance-drift-escalated.v1.schema.json", payload);
    }

    [Fact]
    public void SeatReservationReleased_payload_has_expected_contract()
    {
        object payload = new
        {
            schemaVersion = 1,
            tenantId = Guid.NewGuid(),
            workspaceId = Guid.NewGuid(),
            projectId = Guid.NewGuid(),
            reservationId = Guid.NewGuid(),
            releasedUtc = DateTime.UtcNow,
            releaseReason = "expired"
        };

        AssertPayloadMatchesCommittedSchema("seat-reservation-released.v1.schema.json", payload);
    }

    [Fact]
    public void TrialLifecycleEmail_payload_has_expected_contract()
    {
        object payload = new
        {
            schemaVersion = 1,
            trigger = "midTrialDay7",
            tenantId = Guid.NewGuid(),
            workspaceId = Guid.NewGuid(),
            projectId = Guid.NewGuid(),
            runId = (Guid?)Guid.NewGuid(),
            targetTier = (string?)null
        };

        AssertPayloadMatchesCommittedSchema("trial-lifecycle-email.v1.schema.json", payload);
    }

    [Fact]
    public void BillingMarketplaceWebhookReceived_payload_has_expected_contract()
    {
        object payload = new
        {
            schemaVersion = 1,
            tenantId = Guid.NewGuid(),
            workspaceId = Guid.NewGuid(),
            projectId = Guid.NewGuid(),
            providerDedupeKey = "sub|Subscribe|ABCD1234",
            action = "Subscribe",
            subscriptionId = "guid-from-marketplace",
            billingProvider = "AzureMarketplace"
        };

        AssertPayloadMatchesCommittedSchema("billing-marketplace-webhook-received.v1.schema.json", payload);
    }

    [Fact]
    public void Catalog_entries_match_schema_files_on_disk()
    {
        string integrationEventsDir = Path.Combine(AppContext.BaseDirectory, "schemas", "integration-events");
        Directory.Exists(integrationEventsDir).Should().BeTrue();

        string[] schemaFilesOnDisk = Directory.GetFiles(integrationEventsDir, "*.v1.schema.json")
            .Select(Path.GetFileName)
            .OfType<string>()
            .OrderBy(static f => f, StringComparer.Ordinal)
            .ToArray();

        string catalogPath = Path.Combine(integrationEventsDir, "catalog.json");
        File.Exists(catalogPath).Should().BeTrue();

        string catalogText = File.ReadAllText(catalogPath);
        using JsonDocument catalogDoc = JsonDocument.Parse(catalogText);
        JsonElement catalogRoot = catalogDoc.RootElement;
        JsonElement events = catalogRoot.GetProperty("events");

        Dictionary<string, string> expectedFileToEventType = new(StringComparer.Ordinal)
        {
            ["authority-run-completed.v1.schema.json"] = IntegrationEventTypes.AuthorityRunCompletedV1,
            ["manifest-finalized.v1.schema.json"] = IntegrationEventTypes.ManifestFinalizedV1,
            ["governance-approval-submitted.v1.schema.json"] = IntegrationEventTypes.GovernanceApprovalSubmittedV1,
            ["governance-promotion-activated.v1.schema.json"] =
                IntegrationEventTypes.GovernancePromotionActivatedV1,
            ["alert-fired.v1.schema.json"] = IntegrationEventTypes.AlertFiredV1,
            ["alert-resolved.v1.schema.json"] = IntegrationEventTypes.AlertResolvedV1,
            ["advisory-scan-completed.v1.schema.json"] = IntegrationEventTypes.AdvisoryScanCompletedV1,
            ["compliance-drift-escalated.v1.schema.json"] = IntegrationEventTypes.ComplianceDriftEscalatedV1,
            ["seat-reservation-released.v1.schema.json"] = IntegrationEventTypes.SeatReservationReleasedV1,
            ["trial-lifecycle-email.v1.schema.json"] = IntegrationEventTypes.TrialLifecycleEmailV1,
            ["billing-marketplace-webhook-received.v1.schema.json"] =
                IntegrationEventTypes.BillingMarketplaceWebhookReceivedV1
        };

        HashSet<string> catalogSchemaFiles = new(StringComparer.Ordinal);

        foreach (JsonElement eventEntry in events.EnumerateArray())
        {
            string schemaFile = eventEntry.GetProperty("schemaFile").GetString()!;
            catalogSchemaFiles.Add(schemaFile);

            string schemaPath = Path.Combine(integrationEventsDir, schemaFile);
            File.Exists(schemaPath).Should().BeTrue($"catalog entry references missing file: {schemaFile}");

            eventEntry.GetProperty("schemaVersion").GetInt32().Should().Be(1);

            string eventType = eventEntry.GetProperty("eventType").GetString()!;
            expectedFileToEventType.TryGetValue(schemaFile, out string? expectedType).Should().BeTrue(
                $"unexpected schemaFile in catalog: {schemaFile}");
            eventType.Should().Be(expectedType);
        }

        catalogSchemaFiles.Should().BeEquivalentTo(schemaFilesOnDisk);
    }

    private static void AssertPayloadMatchesCommittedSchema(string schemaFileName, object payload)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "schemas", "integration-events", schemaFileName);

        File.Exists(path).Should().BeTrue($"schema file must be copied to test output: {path}");

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
            foreach (string? name in requiredElement.EnumerateArray().Select(nameElement => nameElement.GetString()))
            {
                name.Should().NotBeNullOrWhiteSpace();

                payloadRoot.TryGetProperty(name, out JsonElement _).Should().BeTrue(
                    $"required property '{name}' must exist in serialized payload for {schemaFileName}");
            }
        }

        if (!schemaRoot.TryGetProperty("properties", out JsonElement propertiesElement)
            || propertiesElement.ValueKind != JsonValueKind.Object)
            return;

        foreach (JsonProperty payloadProperty in payloadRoot.EnumerateObject())
        {
            propertiesElement.TryGetProperty(payloadProperty.Name, out JsonElement _).Should().BeTrue(
                $"serialized payload property '{payloadProperty.Name}' must be declared in schema {schemaFileName} (catch typos; additional payload fields require a schema update)");
        }
    }
}
