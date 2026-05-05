using ArchLucid.Application.DataConsistency;
using ArchLucid.Application.Notifications.Email;
using ArchLucid.Core.Billing;
using ArchLucid.Core.Configuration;
using ArchLucid.Decisioning.Alerts;

namespace ArchLucid.Application.Tests.Integration;

/// <summary>
///     Representative payloads mirroring production anonymous types / DTOs serialized with
///     <see cref="ArchLucid.Core.Integration.IntegrationEventJson.Options" /> (outbox / Service Bus).
/// </summary>
internal static class IntegrationEventWebhookPayloadSampleFactory
{
    internal static object Create(string schemaFile)
    {
        if (string.IsNullOrWhiteSpace(schemaFile))
            throw new ArgumentException("Schema file name is required.", nameof(schemaFile));

        return schemaFile switch
        {
            "authority-run-completed.v1.schema.json" => CreateAuthorityRunCompleted(),
            "manifest-finalized.v1.schema.json" => CreateManifestFinalized(),
            "governance-approval-submitted.v1.schema.json" => CreateGovernanceApprovalSubmitted(),
            "governance-promotion-activated.v1.schema.json" => CreateGovernancePromotionActivated(),
            "alert-fired.v1.schema.json" => CreateAlertFired(),
            "alert-resolved.v1.schema.json" => CreateAlertResolved(),
            "advisory-scan-completed.v1.schema.json" => CreateAdvisoryScanCompleted(),
            "data-consistency-check-completed.v1.schema.json" => CreateDataConsistencyCheckCompleted(),
            "compliance-drift-escalated.v1.schema.json" => CreateComplianceDriftEscalated(),
            "seat-reservation-released.v1.schema.json" => CreateSeatReservationReleased(),
            "trial-lifecycle-email.v1.schema.json" => CreateTrialLifecycleEmail(),
            "billing-marketplace-webhook-received.v1.schema.json" => CreateBillingMarketplaceWebhookReceived(),
            _ => throw new InvalidOperationException(
                $"No JSON sample wired for schema '{schemaFile}'. Add a factory arm when extending catalog.json."),
        };
    }

    /// <remarks>Mirrors <c>AuthorityRunOrchestrator</c> integration payload + finding link rows.</remarks>
    private static object CreateAuthorityRunCompleted()
    {
        Guid runId = Guid.NewGuid();

        return new
        {
            schemaVersion = 1,
            runId,
            manifestId = Guid.NewGuid(),
            tenantId = Guid.NewGuid(),
            workspaceId = Guid.NewGuid(),
            projectId = Guid.NewGuid(),
            previousRunId = (Guid?)Guid.NewGuid(),
            findings = new[]
            {
                new
                {
                    findingId = "finding-primary",
                    deepLinkUrl = $"https://archlucid.net/runs/{runId:D}/findings/finding-primary",
                    severity = "High"
                }
            }
        };
    }

    /// <remarks>Mirrors <c>ManifestFinalizationService</c> outbox payload.</remarks>
    private static object CreateManifestFinalized()
    {
        return new
        {
            schemaVersion = 1,
            runId = Guid.NewGuid(),
            manifestId = Guid.NewGuid(),
            decisionTraceId = Guid.NewGuid(),
            tenantId = Guid.NewGuid(),
            workspaceId = Guid.NewGuid(),
            projectId = Guid.NewGuid(),
            findingsSnapshotId = Guid.NewGuid(),
            artifactBundleId = (Guid?)Guid.NewGuid(),
            manifestVersion = "v1"
        };
    }

    /// <remarks>Mirrors <c>GovernanceWorkflowService.TryPublishGovernanceApprovalSubmittedAsync</c>.</remarks>
    private static object CreateGovernanceApprovalSubmitted()
    {
        return new
        {
            schemaVersion = 1,
            tenantId = Guid.NewGuid(),
            workspaceId = Guid.NewGuid(),
            projectId = Guid.NewGuid(),
            approvalRequestId = "approval-req-1",
            runId = Guid.NewGuid().ToString("D"),
            manifestVersion = "v1",
            sourceEnvironment = "dev",
            targetEnvironment = "test",
            requestedBy = "user-1"
        };
    }

    /// <remarks>Mirrors <c>GovernanceWorkflowService.TryPublishGovernancePromotionActivatedAsync</c>.</remarks>
    private static object CreateGovernancePromotionActivated()
    {
        return new
        {
            schemaVersion = 1,
            tenantId = Guid.NewGuid(),
            workspaceId = Guid.NewGuid(),
            projectId = Guid.NewGuid(),
            activationId = "activation-1",
            runId = Guid.NewGuid().ToString("D"),
            manifestVersion = "v1",
            environment = "prod",
            activatedBy = "user-1",
            activatedUtc = DateTime.UtcNow
        };
    }

    /// <remarks>Mirrors <c>AlertIntegrationEventPublishing.TryPublishFiredAsync</c>.</remarks>
    private static object CreateAlertFired()
    {
        AlertRecord alert = new()
        {
            AlertId = Guid.NewGuid(),
            RuleId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            ComparedToRunId = null,
            Title = "title",
            Category = "category",
            Severity = "High",
            DeduplicationKey = "dedupe-key"
        };

        return new
        {
            schemaVersion = 1,
            tenantId = alert.TenantId,
            workspaceId = alert.WorkspaceId,
            projectId = alert.ProjectId,
            alertId = alert.AlertId,
            runId = alert.RunId,
            comparedToRunId = alert.ComparedToRunId,
            ruleId = alert.RuleId,
            category = alert.Category,
            severity = alert.Severity,
            title = alert.Title,
            deduplicationKey = alert.DeduplicationKey
        };
    }

    /// <remarks>Mirrors <c>AlertIntegrationEventPublishing.TryPublishResolvedAsync</c>.</remarks>
    private static object CreateAlertResolved()
    {
        AlertRecord alert = new()
        {
            AlertId = Guid.NewGuid(),
            RuleId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            RunId = null,
            DeduplicationKey = "composite:rule:scope"
        };

        return new
        {
            schemaVersion = 1,
            tenantId = alert.TenantId,
            workspaceId = alert.WorkspaceId,
            projectId = alert.ProjectId,
            alertId = alert.AlertId,
            runId = alert.RunId,
            deduplicationKey = alert.DeduplicationKey,
            resolvedByUserId = "resolver-1",
            comment = (string?)null
        };
    }

    /// <remarks>Mirrors <c>AdvisoryScanRunner.TryPublishAdvisoryScanCompletedAsync</c>.</remarks>
    private static object CreateAdvisoryScanCompleted()
    {
        return new
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
    }

    /// <remarks>Mirrors <c>DataConsistencyReconciliationHostedService.TryPublishCompletedEventAsync</c>.</remarks>
    private static object CreateDataConsistencyCheckCompleted()
    {
        DataConsistencyReport report = new(
            CheckedAtUtc: DateTime.UtcNow,
            Findings:
            [
                new DataConsistencyFinding(
                    CheckName: "sample-check",
                    Severity: DataConsistencyFindingSeverity.Warning,
                    Description: "sample description",
                    AffectedEntityIds: ["entity-1", "entity-2"])
            ],
            IsHealthy: true);

        return new
        {
            report.CheckedAtUtc,
            report.IsHealthy,
            Findings = report.Findings
                .Select(f => new
                {
                    f.CheckName,
                    Severity = f.Severity.ToString(),
                    f.Description,
                    AffectedEntityIds = f.AffectedEntityIds.Take(50).ToArray()
                })
                .ToArray()
        };
    }

    /// <summary>Catalog event; publisher wiring may land outside Application — shape kept aligned with committed schema.</summary>
    private static object CreateComplianceDriftEscalated()
    {
        return new
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
    }

    /// <summary>Catalog event; publisher wiring may land outside Application — shape kept aligned with committed schema.</summary>
    private static object CreateSeatReservationReleased()
    {
        return new
        {
            schemaVersion = 1,
            tenantId = Guid.NewGuid(),
            workspaceId = Guid.NewGuid(),
            projectId = Guid.NewGuid(),
            reservationId = Guid.NewGuid(),
            releasedUtc = DateTime.UtcNow,
            releaseReason = "expired"
        };
    }

    /// <remarks>Mirrors <c>TrialLifecycleIntegrationEventPublisher</c> body (<see cref="TrialLifecycleEmailIntegrationEnvelope" />).</remarks>
    private static object CreateTrialLifecycleEmail()
    {
        return new TrialLifecycleEmailIntegrationEnvelope
        {
            SchemaVersion = 1,
            Trigger = TrialLifecycleEmailTrigger.MidTrialDay7,
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            TargetTier = null
        };
    }

    /// <remarks>Mirrors <c>MarketplaceWebhookIntegrationEventPublisher</c> anonymous body.</remarks>
    private static object CreateBillingMarketplaceWebhookReceived()
    {
        MarketplaceWebhookReceivedIntegrationPayload payload = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            ProviderDedupeKey = "sub|Subscribe|ABCD1234",
            Action = "Subscribe",
            SubscriptionId = "guid-from-marketplace",
            BillingProvider = BillingProviderNames.AzureMarketplace
        };

        return new
        {
            schemaVersion = 1,
            tenantId = payload.TenantId,
            workspaceId = payload.WorkspaceId,
            projectId = payload.ProjectId,
            providerDedupeKey = payload.ProviderDedupeKey,
            action = payload.Action,
            subscriptionId = payload.SubscriptionId,
            billingProvider = payload.BillingProvider
        };
    }
}
