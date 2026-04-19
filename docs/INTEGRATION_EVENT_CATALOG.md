# Integration event catalog (ArchLucid)

**Audience:** platform engineers wiring **Azure Service Bus** consumers, SIEM pipelines, or partner automation.

**Canonical types:** `ArchLucid.Core.Integration.IntegrationEventTypes` (`com.archlucid.*` strings).

**Machine-readable catalog:** `schemas/integration-events/catalog.json` (validated in CI by `IntegrationEventCatalogSyncTests`). **Registry narrative:** [INTEGRATION_EVENT_SCHEMA_REGISTRY.md](INTEGRATION_EVENT_SCHEMA_REGISTRY.md).

## Wire shape

| Layer | Behavior |
|--------|----------|
| **Outbox** | Domain commits enqueue rows; worker publishes UTF-8 JSON payloads via `IIntegrationEventPublisher`. |
| **Payload** | Serialized with `IntegrationEventJson.Options` (camelCase, omit nulls). **There is no separate envelope type in code** — the **Service Bus application property** or **subject** carries `eventType`; the **body** is the payload object for that event. |
| **Idempotency** | Publishers use deterministic **`messageId`** strings (e.g. `{entityId}:{eventType}`) where implemented — consumers should dedupe on `messageId` + payload hash. |

## Event types (summary)

| Constant | String value | Typical producer |
|----------|----------------|------------------|
| `AuthorityRunCompletedV1` | `com.archlucid.authority.run.completed` | `AuthorityRunOrchestrator` |
| `GovernanceApprovalSubmittedV1` | `com.archlucid.governance.approval.submitted` | `GovernanceWorkflowService` |
| `GovernancePromotionActivatedV1` | `com.archlucid.governance.promotion.activated` | `GovernanceWorkflowService` |
| `AlertFiredV1` | `com.archlucid.alert.fired` | `AlertIntegrationEventPublishing` |
| `AlertResolvedV1` | `com.archlucid.alert.resolved` | `AlertIntegrationEventPublishing` |
| `AdvisoryScanCompletedV1` | `com.archlucid.advisory.scan.completed` | `AdvisoryScanRunner` |
| `TrialLifecycleEmailV1` | `com.archlucid.notifications.trial-lifecycle-email.v1` | Trial lifecycle scanner / domain transitions |
| `BillingMarketplaceWebhookReceivedV1` | `com.archlucid.billing.marketplace.webhook.received.v1` | `BillingMarketplaceWebhookController` after `AzureMarketplaceBillingProvider` processes a webhook |

**Wildcard:** `IntegrationEventTypes.WildcardEventType` (`*`) is for the **logging** handler only, not for publishers.

## Consumer guidance

1. **Subscribe** to the integration topic configured in Terraform (`infra/terraform-servicebus/`) and filter by event type.
2. **Parse** the body as JSON per the matching file under `schemas/integration-events/` (see `catalog.json` → `schemaFile`).
3. **Handle duplicates** — at-least-once delivery; use idempotent handlers.
4. **Dead-letter** — follow `docs/INTEGRATION_EVENTS_AND_WEBHOOKS.md` and worker DLQ tooling for poison messages.

## Related

- `docs/INTEGRATION_EVENTS_AND_WEBHOOKS.md`
- `docs/ARCHITECTURE_INDEX.md`
- `ArchLucid.Core.Tests/Integration/IntegrationEventCatalogSyncTests.cs`
